/*
START LICENSE DISCLAIMER
Starwatch is a Starbound Server manager with player management, crash recovery and a REST and websocket (live) API. 
Copyright(C) 2022 Lachee

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
using Starwatch.Entities;
using Starwatch.API.Rest.Serialization;
using Starwatch.API.Web;
using Starwatch.API.Gateway.Event;
using Starwatch.API.Gateway;

namespace Starwatch.API.Rest.Route
{
    [Route("/ban/:ticket", AuthLevel.Admin)]
    class BanDetailsRoute : RestRoute, IGatewayRoute
    {
        [Argument("ticket", Converter = typeof(BanConverter))]
        public Ban Ban { get; set; }

        public override Type PayloadType => typeof(Ban);

        public BanDetailsRoute(RestHandler handler, Authentication authentication) : base(handler, authentication) { }

        public BanDetailsRoute(Ban ban) : base(null, null) => Ban = ban;

        public string GetRouteName() => "/ban/:ticket";

        public void SetGateway(EventConnection gateway)
        {
            Handler = gateway.API.RestHandler;
            Authentication = gateway.Authentication;
        }

        /// <summary>
        /// Gets a ban
        /// </summary>
        public override RestResponse OnGet(Query query)
        => new RestResponse(RestStatus.OK, res: Ban);

        /// <summary>
        /// Deletes a ban
        /// </summary>
        public override RestResponse OnDelete(Query query)
        {
            //Failed to remove the ban
            if (!Starbound.Configurator.ExpireBanAsync(Ban).Result)
                return new RestResponse(RestStatus.OK, res: false);

            //Save the settings and reload
            var task = Starbound.SaveConfigurationAsync(true);
            return query.GetBool(Query.AsyncKey, false) 
                ? RestResponse.Async 
                : new RestResponse(RestStatus.OK, res: task.Result);
        }
    }
}
