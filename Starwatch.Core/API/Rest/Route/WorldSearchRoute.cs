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
using Starwatch.API.Web;
using Starwatch.API.Rest.Routing;
using Starwatch.Entities;
using Starwatch.API.Rest.Serialization;
using System.Collections.Generic;

namespace Starwatch.API.Rest.Route
{
    [Route("/world/search", AuthLevel.Bot)]
    class WorldSearchRoute : RestRoute
    {
        public WorldSearchRoute(RestHandler handler, Authentication authentication) : base(handler, authentication) { }

        /// <summary>
        /// Gets the specific details about the world
        /// </summary>
        public override RestResponse OnGet(Query query)
        {
            var results = CelestialWorld.SearchCoordinatesAsync(DbContext,
                query.GetOptionalLong("xmin"),
                query.GetOptionalLong("xmax"),
                query.GetOptionalLong("ymin"),
                query.GetOptionalLong("ymax")
                ).Result;

            if (query.GetBool("systems", false))
            {
                //Group them in to systems
                Dictionary<Coordinate, List<World>> grouping = new Dictionary<Coordinate, List<World>>();
                foreach (var r in results)
                {
                    var coord = new Coordinate()
                    {
                        x = r.X,
                        y = r.Y
                    };

                    if (!grouping.ContainsKey(coord))
                        grouping.Add(coord, new List<World>());

                    grouping[coord].Add(r);
                }

                return new RestResponse(RestStatus.OK, res: grouping);
            }


            return new RestResponse(RestStatus.OK, res: results);
        }
     
        struct Coordinate
        {
            public long x, y;
            public override string ToString()
            {
                return $"{x},{y}";
            }
        }

    }
}
