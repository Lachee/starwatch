using Starwatch.API.Util;
using Starwatch.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using WebSocketSharp.Server;

namespace Starwatch.API.Web
{
    public class LogHandler : IRequestHandler
    {
        private const string ROOT_URL = "log/";
        private const string BASE_LOG = "starbound_server.log";

        public Logger Logger { get; }
        public bool ShowListing { get; set; } = false;
        public ApiHandler ApiHandler { get; }
        public Starbound.Server Starbound => ApiHandler.Starwatch.Server;

        public LogHandler(ApiHandler apiHandler)
        {
            ApiHandler = apiHandler;
            Logger = new Logger("LOG", ApiHandler.Logger);
        }

        public bool HandleRequest(RequestMethod method, HttpRequestEventArgs args, Authentication auth)
        {
            //This isnt a auth request
            if (args.Request.Url.Segments.Length < 2) return false;
            if (args.Request.Url.Segments[1] != ROOT_URL) return false;

            //Admin users only. Bots are forbidden.
            if (!auth.IsAdmin)
            {
                args.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                args.Response.Close();
                return true;
            }

            //We are requesting the root directory.
            if (args.Request.Url.Segments.Length >= 3)
            {
                //If we are search then do a handle search
                Query query = new Query(args.Request);
                if (query.ContainsKey("regex"))     HandleSearchRequest(method, args, auth, query);
                else                                HandleFileRequest(method, args, auth);

                return true;
            }
            
            if (ShowListing)
            {
                HandleDirectoryRequest(method, args, auth);
                return true;
            }

            return false;
        }

        private void HandleDirectoryRequest(RequestMethod method, HttpRequestEventArgs args, Authentication auth)
        {
            auth.RecordAction("log:dirlist");
            Logger.Log(auth + " is searching log directory.");

            //Prepare the page
            StringBuilder html = new StringBuilder();
            foreach (var file in Directory.EnumerateFiles(Starbound.StorageDirectory, BASE_LOG + "*"))
            {
                string extension = Path.GetExtension(file);
                string number = (extension.Equals(".log") ? "0" : extension.Remove(0, 1));
                html.Append($"<a href='/{ROOT_URL}{number}'>Log {number}</a><br>");
            }

            //Respond with the HTML
            args.Response.WriteText(html.ToString(), ContentType.HTML);
        }

        private void HandleFileRequest(RequestMethod method, HttpRequestEventArgs args, Authentication auth)
        {
            //Get which log we are accessing and store the action
            string log = args.Request.Url.Segments[2];
            auth.RecordAction($"log:{log}");
            Logger.Log(auth + " is viewing log " + log);

            //Generate the path, appending the log if its not 0
            string path = $"{Starbound.StorageDirectory}/{BASE_LOG}";
            if (log != "0") path += $".{log}";

            //Make sure it exists
            if (!File.Exists(path))
            {
                args.Response.StatusCode = (int)HttpStatusCode.NotFound;
                args.Response.Close();
                return;
            }

            //Return the file with a ReadWrite share.
            args.Response.WriteFile(path, FileShare.ReadWrite);
        }

        private void HandleSearchRequest(RequestMethod method, HttpRequestEventArgs args, Authentication auth, Query query)
        {
            string search = query.GetString("regex", "\\w*");
            string log = query.GetString("log", "0");

            //Doubling up because this is an expensive action!
            auth.RecordAction($"log:{log}:search:{search}");
            auth.IncrementRateLimit();

            Logger.Log(auth + " search log " + log + " with '"+ search +"'");

            //Create the regex
            Regex regex = new Regex(search);

            //Generate the path, appending the log if its not 0
            string path = $"{Starbound.StorageDirectory}/{BASE_LOG}";
            if (log != "0") path += $".{log}";

            //Make sure it exists
            if (!File.Exists(path))
            {
                args.Response.StatusCode = (int)HttpStatusCode.NotFound;
                args.Response.Close();
                return;
            }

            //Do the actual searching with a string builder to generate some HTML
            StringBuilder html = new StringBuilder();
            //html.Append("<html>");
            //html.Append("<head>");
            //html.Append("<title>Log Search</title>");
            //html.Append("<base href='../'>");
            //html.Append("<link rel='stylesheet' type='text/css' href='css/main.css'>");
            //html.Append("<link rel='stylesheet' type='text/css' href='css/log.css'>");
            //html.Append("</head><body>");

            int preMatchSize = 5;
            int postMatchSize = 3;
            long lastMatchLine = -100;
            Queue<string> searchQueue = new Queue<string>(preMatchSize + 1);
            
            long lineCount = 0;
            long matchCount = 0;

            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (StreamReader reader = new StreamReader(stream))
                {                   
                    while (reader.Peek() > 0)
                    {
                        //Fetch the line
                        string line = reader.ReadLine();
                        lineCount++;

                        //Calc when the end is
                        long endOfMatchLine = lastMatchLine + postMatchSize;

                        //Do some matching
                        var match = regex.Match(line);
                        if (match.Success)
                        {
                            //Add the match header
                            if (lineCount > endOfMatchLine) html.Append($"<table border='1' id='Match{matchCount++}'>");
                            //html.Append("<tr><th>Line</th><th>Text</th></tr>");

                            //Add the previous 5 elements
                            long templine = lineCount - searchQueue.Count;
                            foreach (var s in searchQueue)
                            {
                                html.Append($"<tr id='onion'><td id='no'>{templine++}</td><td id='line'>{s.StripTags()}</td></tr>");
                            }

                            //Empty the queue
                            searchQueue.Clear();

                            //Add ourself
                            string center = line.Substring(match.Index, match.Length).StripTags();
                            string prefix = line.Substring(0, match.Index).StripTags();
                            string suffix = line.Substring(match.Index + match.Length).StripTags();
                            
                            //Append
                            html.Append($"<tr id='correct'><td id='no'>{lineCount}</td>");
                            html.Append($"<td id='line'><span id='pre'>{prefix}</span><span id='highlight'>{center}</span><span id='post'>{suffix}</span></td>");
                            html.Append("</tr>");
                            lastMatchLine = lineCount;
                        }
                        else
                        {
                            //If we are too close, then we will just log it. Otherwise we will queue it
                            if (lineCount <= endOfMatchLine)
                            {
                                //append the line. If we are at the end, append a break
                                html.Append($"<tr id='onion'><td id='no'>{lineCount}</td><td id='line'>{line.StripTags()}</td></tr>");
                                if (lineCount == endOfMatchLine) html.Append("</table>");
                            }
                            else
                            {
                                //Add the line to the queue
                                searchQueue.Enqueue(line);
                                if (searchQueue.Count > preMatchSize) searchQueue.Dequeue();
                            }
                        }
                    }
                }
            }

            html.Append("</table>");
            args.Response.WriteText(html.ToString(), ContentType.HTML);
        }




    }
}
