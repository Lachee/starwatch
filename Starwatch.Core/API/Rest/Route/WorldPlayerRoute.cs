using System;
using System.Collections.Generic;
using Starwatch.API.Rest.Routing;
using Starwatch.API.Web;
using Starwatch.Entities;
using Starwatch.Starbound;
using Starwatch.API;
using Starwatch.API.Rest.Serialization;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace Starwatch.API.Rest.Route
{
    [Route("/world/:identifier/players")]
    class WorldPlayerRoute : RestRoute
    {
        [Argument("identifier", Converter = typeof(WorldConverter))]
        public World World { get; set; }

        public WorldPlayerRoute(RestHandler handler, Authentication authentication) : base(handler, authentication) { }

        /// <summary>
        /// Gets all the players on the world
        /// </summary>
        public override RestResponse OnGet(Query query)
        {
            var players = Starbound.Connections.GetPlayersEnumerator()
                .Where(p => p.Location.Equals(World))
                .ToDictionary(p => (p.Connection, p.Username));
            return new RestResponse(RestStatus.OK, res: players);
        }        
    }
}
