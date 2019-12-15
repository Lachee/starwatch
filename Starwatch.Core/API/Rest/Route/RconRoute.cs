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
using Starwatch.Starbound.Rcon;
using System.Threading.Tasks;

namespace Starwatch.API.Rest.Route
{
    [Route("/rcon", permission: AuthLevel.SuperBot)]
    class RconRoute : RestRoute
    {
        public override Type PayloadType => typeof(Payload);
        struct Payload
        {
            public string Command { get; set; }
        }

        public RconRoute(RestHandler handler, Authentication authentication) : base(handler, authentication) { }
        public override RestResponse OnPost(Query query, object payloadObject)
        {
            Payload payload = (Payload)payloadObject;
            bool async = query.GetBool(Query.AsyncKey, false);

            if (Starbound.Rcon == null)
                return new RestResponse(RestStatus.BadRequest, "Rcon not enabled.");

            //Prepare the task
            Handler.Logger.LogWarning(Authentication + " is using RCON: " + payload.Command);
            Task<RconResponse> task = Starbound.Rcon.ExecuteAsync(payload.Command);

            //If we are async then return asap, otherwise get the result
            if (async) return RestResponse.Async;
            return new RestResponse(RestStatus.OK, res: task.Result);
        }
    }
}
