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
using System;
using Starwatch.API.Rest.Routing;

using Starwatch.API.Web;

namespace Starwatch.API.Rest.Route
{
    [Route("/chat", AuthLevel.Admin)]
    class ChatRoute : RestRoute
    {
        public override Type PayloadType => typeof(Payload);
        struct Payload
        {
            public string Content { get; set; }
        }

        public ChatRoute(RestHandler handler, Authentication authentication) : base(handler, authentication) { }

        public override RestResponse OnPost(Query query, object payloadObject)
        {
            if (Starbound.Rcon is null)
                return new RestResponse(RestStatus.BadRequest, "Rcon not enabled.");

            var payload = (Payload)payloadObject;
            string message = payload.Content;

            if (query.GetBool("include_tag", false))
                message = $"<^pink;{Authentication.Name}^reset;> {message}";

            var task = Starbound.Rcon.BroadcastAsync(message);
            return query.GetBool(Query.AsyncKey, false) 
                ? RestResponse.Async 
                : new RestResponse(RestStatus.OK, res: task.Result);
        }
    }
}
