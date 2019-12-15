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
using Starwatch.API.Rest.Routing;
using Starwatch.API.Web;
using System.Linq;
using Starwatch.API.Gateway.Event;
using Starwatch.API.Gateway;

namespace Starwatch.API.Rest.Route
{
    [Route("/player/all", AuthLevel.Admin)]
    class PlayerAllRoute : RestRoute, IGatewayRoute
    {
        public bool IsGateway { get; private set; }

        public PlayerAllRoute(RestHandler handler, Authentication authentication) : base(handler, authentication) { IsGateway = false; }

        public string GetRouteName() => "/player/all";

        public void SetGateway(EventConnection gateway)
        {
            this.Handler = gateway.API.RestHandler;
            this.Authentication = gateway.Authentication;
            this.IsGateway = true;
        }

        public override RestResponse OnGet(Query query)
        {
            //if (!IsGateway && AuthenticationLevel < AuthLevel.SuperBot)
            //    return RestResponse.Forbidden;

            var players = Starbound.Connections.GetCopiedPlayersEnumerable();
            return new RestResponse(RestStatus.OK, res: players.ToArray());
        }

        
    }
}
