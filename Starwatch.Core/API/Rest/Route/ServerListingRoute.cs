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
using Newtonsoft.Json.Linq;
using Starwatch.API.Gateway;
using Starwatch.API.Gateway.Event;
using Starwatch.API.Rest.Routing;
using Starwatch.API.Web;

namespace Starwatch.API.Rest.Route
{
    [Route("/server/statistics/listing", AuthLevel.Bot)]
    class ServerListingRoute : RestRoute
    {
        public override bool Silent => true;

        public ServerListingRoute(RestHandler handler, Authentication authentication) : base(handler, authentication) { }


        public override RestResponse OnGet(Query query)
        {
            var stat = new CompoundStatistic
            {
                Statistics = Handler.Starbound.GetStatisticsAsync().Result,
                Configurator = Handler.Starbound.Configurator
            };

            return new RestResponse(RestStatus.OK, res: stat);
        }

        class CompoundStatistic
        {
            public Starbound.Statistics Statistics { get; set; }
            public Starbound.Configurator Configurator { get; set; }
        }


    }
}
