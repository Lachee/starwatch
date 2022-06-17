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
using Starwatch.API.Gateway;
using Starwatch.API.Rest.Routing;
using System.Collections.Generic;
using System.Linq;
using Starwatch.API.Web;

namespace Starwatch.API.Rest.Route
{
    [Route("/meta/gateway", AuthLevel.SuperUser)]
    class MetaGatewayRoute : RestRoute
    {
        public MetaGatewayRoute(RestHandler handler, Authentication authentication) : base(handler, authentication) { }

        public override RestResponse OnGet(Query query)
        {
            WebSocketSharp.Server.WebSocketServiceHost serviceHost;
            var gateways = new List<GatewayConnection>();

            serviceHost = Handler.HttpServer.WebSocketServices["/log"];
            gateways.AddRange(serviceHost.Sessions.Sessions.Select(s => s as LogConnection));

            serviceHost = Handler.HttpServer.WebSocketServices["/events"];
            gateways.AddRange(serviceHost.Sessions.Sessions.Select(s => s as EventConnection));

            //TODO: Make this list all sessions, not just last one
            return new RestResponse(RestStatus.OK, res: new GatewayListing()
            {
                ActiveSessions = serviceHost.Sessions.ActiveIDs.Count(),
                InactiveSessions = serviceHost.Sessions.InactiveIDs.Count(),
                Connections = gateways.ToArray()
            });
        }


        struct GatewayListing
        {
            public int ActiveSessions { get; set; }
            public int InactiveSessions { get; set; }
            public GatewayConnection[] Connections { get; set; }
        }
    }
}
