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
using System.Linq;

namespace Starwatch.API.Rest.Route
{
    [Route("/world")]
    class WorldRoute : RestRoute
    {
        public WorldRoute(RestHandler handler, Authentication authentication) : base(handler, authentication) { }

        /// <summary>
        /// Gets a list of worlds players are current occupying
        /// </summary>
        public override RestResponse OnGet(Query query)
        {
            var worlds = Starbound.Connections.GetCopiedPlayersEnumerable()
                .Select(p => p.Location?.Whereami)
                .Distinct()
                .ToArray();

            return new RestResponse(RestStatus.OK, res: worlds);
        }
    }
}
