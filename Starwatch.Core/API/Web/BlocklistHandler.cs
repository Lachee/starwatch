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
using System.Collections.Generic;
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
