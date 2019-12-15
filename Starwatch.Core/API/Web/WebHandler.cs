/*
START LICENSE DISCLAIMER
Starwatch is a Starbound Server manager with player management, crash recovery and a REST and websocket (live) API. 
Copyright(C) 2020 Lachee

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published
by the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with this program. If not, see < https://www.gnu.org/licenses/ >.
END LICENSE DISCLAIMER
*/
using Starwatch.API.Util;
using System.IO;
using System.Linq;
using System.Net;
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
