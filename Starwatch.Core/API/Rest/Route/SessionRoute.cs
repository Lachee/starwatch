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
using System;
using System.Collections.Generic;
using System.Linq;
using Starwatch.API.Web;
using System.Text;
using Starwatch.Starbound;

namespace Starwatch.API.Rest.Route
{
    [Route("/session", AuthLevel.Admin)]
    class SessionRoute : RestRoute
    {
        //TODO: Implement Deep Analyisis
        public SessionRoute(RestHandler handler, Authentication authentication) : base(handler, authentication) { }
        public override RestResponse OnGet(Query query)
        {
            var parameters = new Dictionary<string, object>();
            if (query.TryGetValue("username", out var username))
            {
                //parameters.Add("username",       $"%{username}%");
                parameters.Add("username_clean", $"%{username}%");
            }

            if (query.TryGetValue("account", out var account))
                parameters.Add("account", $"%{account}%");

            if (query.TryGetValue("ip", out var ip))
                parameters.Add("ip", ip);

            if (query.TryGetValue("uuid", out var uuid))
                parameters.Add("uuid", $"%{uuid}%");

            if (query.TryGetValue("connection", out var connection))
                parameters.Add("cid", connection);

            TimeSpan? timespan = null;
            if (query.TryGetInt("seconds", out var second )) timespan = TimeSpan.FromSeconds(second);
            if (query.TryGetInt("minutes", out var minutes)) timespan = TimeSpan.FromMinutes(minutes);
            if (query.TryGetInt("hours",   out var hours  )) timespan = TimeSpan.FromHours  (hours);
            if (query.TryGetInt("days",    out var days   )) timespan = TimeSpan.FromDays   (days);

            var result = Session.FindAsync(Starbound.DbContext, Starbound, parameters, mode: "AND", duration: timespan).Result;
            return new RestResponse(RestStatus.OK, result);
        }
        
    }
}
