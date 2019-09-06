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
        public bool ShowListing { get; set; } = true;
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
            if (!auth.IsAdmin || auth.IsBot)
            {
                args.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                args.Response.Close();
                return true;
            }


            // 0    1    2
            // api/ log/ 1/
            //We are requesting a specific log
            if (args.Request.Url.Segments.Length >= 3)
            {
                //If we are search then do a handle search
                Query query = new Query(args.Request);
                HandleFileRequest(method, args, auth, query.GetString("regex", null), query.GetInt("onion", 10));
                return true;
            }
            
            //We are to show the directory listing instead.
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

        private void HandleFileRequest(RequestMethod method, HttpRequestEventArgs args, Authentication auth, string pattern = null, int onion = 10)
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

            if (string.IsNullOrEmpty(pattern))
            {
                //We we have no pattern, then just return the file contents directly
                args.Response.WriteFile(path, FileShare.ReadWrite);
            }
            else
            {
                //Lets scan each file and return its response
                //We are incrementing our rate limit twice because its an expensive operation.
                Logger.Log(auth + " search log " + log + " with '" + pattern + "'");
                auth.RecordAction($"log:{log}:search:{log}");
                auth.IncrementRateLimit();

                string content = ScanFile(path, pattern, onion);
                args.Response.WriteText(content, ContentType.HTML);
            }
        }

        /// <summary>
        /// Scans a file, producing a XML Table of matches, with onions.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="pattern"></param>
        /// <param name="onion"></param>
        /// <returns></returns>
        private string ScanFile(string file, string pattern, int onion)
        {
            //Create the regex
            Regex regex = new Regex(pattern);
            StringBuilder html = new StringBuilder("<matches>");

            string line;

            int preMatchSize    = (int) Math.Ceiling(onion / 2.0);
            int postMatchSize   = (int) Math.Floor(onion / 2.0); ;
            long lastMatchLine = -100;
            Queue<string> searchQueue = new Queue<string>(preMatchSize + 1);
            
            long lineCount = 0;
            long matchCount = 0;

            using (FileStream stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (StreamReader reader = new StreamReader(stream))
                {     
                    while (reader.Peek() > 0)
                    {
                        //Fetch the line
                        line = reader.ReadLine();
                        lineCount++;

                        //Calc when the end is
                        long endOfMatchLine = lastMatchLine + postMatchSize;

                        //Do some matching
                        var match = regex.Match(line);
                        if (match.Success)
                        {
                            //Add the match header
                            if (lineCount > endOfMatchLine) html.Append($"<table id='{matchCount++}'>");
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

            html.Append("</table>").Append("</matches>");
            return html.ToString();
        }




    }
}
