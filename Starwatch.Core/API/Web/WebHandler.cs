using Starwatch.API.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using WebSocketSharp.Server;

namespace Starwatch.API.Web
{
    public class WebHandler : IRequestHandler
    {
        public enum SearchMode { None, HTML, AnyFile }
        public SearchMode MissingExtensionMode { get; set; } = SearchMode.HTML;
        public string ContentRoot { get; set; }
        public WebHandler(ApiHandler apiHandler)
        {

        }

        public bool HandleRequest(RequestMethod method, HttpRequestEventArgs args, Authentication auth)
        {
            //Bad Method
            if (method != RequestMethod.Get)
            {
                args.Response.StatusCode = (int) HttpStatusCode.MethodNotAllowed;
                args.Response.Close();
                return true;
            }

            //Make sure the resource URL is good.
            string resource = args.Request.Url.AbsolutePath.TrimStart('/');
            if (string.IsNullOrWhiteSpace(resource)) resource = "index.html";

            //Makesure directory exists (for debugging)
            if (!Directory.Exists(ContentRoot))
                Directory.CreateDirectory(ContentRoot);

            //Get the actual content
            string path = Path.Combine(ContentRoot, resource);
            auth.RecordAction($"web:{path}");

            //Make sure it has a extension
            string ext = Path.GetExtension(path);
            if (!string.IsNullOrEmpty(ext))
            {
                //The file has a extension, so just return it normally
                //Return the file
                args.Response.WriteFile(path);
                return true;
            }
            else
            {
                //If we end with a forward slash its straight up directory
                if (resource.EndsWith("/")) return false;

                switch(MissingExtensionMode)
                {
                    default:
                    case SearchMode.None:
                        return false;

                    case SearchMode.HTML:
                        args.Response.WriteFile(path + ".html");
                        return true;

                    case SearchMode.AnyFile:
                        string file = Directory.EnumerateFiles(ContentRoot, resource + ".*").FirstOrDefault();
                        args.Response.WriteFile(file);
                        return true;
                }
                
            }
        }
    }
}
