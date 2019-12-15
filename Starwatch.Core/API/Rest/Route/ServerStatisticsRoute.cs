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
using Starwatch.API.Gateway.Event;
using Starwatch.API.Rest.Routing;
using Starwatch.API.Web;

namespace Starwatch.API.Rest.Route
{
    [Route("/server/statistics")]
    class ServerStatisticsRoute : RestRoute, IGatewayRoute
    {
        public override bool Silent => true;

        public ServerStatisticsRoute(RestHandler handler, Authentication authentication) : base(handler, authentication) { }
        public ServerStatisticsRoute() { }

        public string GetRouteName() => "/server/statistics";
        public void SetGateway(EventConnection gateway)
        {
            Handler = gateway.API.RestHandler;
            Authentication = gateway.Authentication;
        }

        public override RestResponse OnGet(Query query)
        {
            var task = Handler.Starbound.GetStatisticsAsync();
            task.Wait();
            return new RestResponse(RestStatus.OK, res: task.Result);
        }


    }
}
