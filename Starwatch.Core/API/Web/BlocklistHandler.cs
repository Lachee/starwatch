using Starwatch.API.Util;
using System;
using System.Collections.Generic;
using System.Text;
using WebSocketSharp.Net;
using WebSocketSharp.Server;

namespace Starwatch.API.Web
{
    public class BlocklistHandler : IRequestHandler
    {
        public HashSet<string> BlockAddresses { get; set; }
        public bool HandleRequest(RequestMethod method, HttpRequestEventArgs args, Authentication auth)
        {
            if (BlockAddresses.Contains(args.Request.UserHostAddress) || BlockAddresses.Contains(auth.Name))
            {
                args.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                args.Response.WriteText("“I’m sorry. I know who you are–I believe who you say you are–but you just don’t have permission to access this resource. Maybe if you ask the system administrator nicely, you’ll get permission. But please don’t bother me again until your predicament changes.”");
                return true;
            }

            return false;
        }
    }
}
