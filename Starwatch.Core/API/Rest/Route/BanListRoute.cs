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
using Starwatch.Entities;
using Starwatch.API.Web;

namespace Starwatch.API.Rest.Route
{
    [Route("/ban/list", AuthLevel.Admin)]
    class BanListRoute : RestRoute
    {
        public BanListRoute(RestHandler handler, Authentication authentication) : base(handler, authentication) { }

        /// <summary>
        /// Returns a list of bans.
        /// 
        /// GET Parameters are:
        ///   - limit=1-50 - How many bans are returned
        ///   - page=0-etc. - Which page of bans
        /// </summary>
        public override RestResponse OnGet(Query query)
        {
            int page = 0;
            int limit = 10;
            bool success = false;
            
            if (query.ContainsKey("limit"))
            {
                success = int.TryParse(query["limit"], out limit);
                if (!success)
                {
                    limit = 10;
                }

                limit = Math.Min(50, limit);
                limit = Math.Max(1, limit);
            }

            if (query.ContainsKey("page"))
            {
                success = int.TryParse(query["page"], out page);
                if (!success)
                {
                    page = 0;
                }

                page = Math.Max(0, page);
            }

            string sql = $"SELECT * FROM !bans ORDER BY ticket DESC LIMIT {page * limit},{limit}";

            var res = Starbound.Configurator.DbContext.ExecuteAsync(
                sql,
                Ban.FromDbDataReader
            ).Result;

            return new RestResponse(RestStatus.OK, res);
        }
    }
}
