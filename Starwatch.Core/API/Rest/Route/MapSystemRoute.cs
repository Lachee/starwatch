using Starwatch.API.Web;
using Starwatch.API.Rest.Routing;
using Starwatch.Entities;
using Starwatch.API.Rest.Serialization;
using System.Collections.Generic;

namespace Starwatch.API.Rest.Route
{
    [Route("/map/:system", AuthLevel.User)]
    class MapSystemRoute : RestRoute
    {
        [Argument("system", Converter = typeof(WorldConverter))]
        public SystemWorld SystemWorld { get; set; }

        public MapSystemRoute(RestHandler handler, Authentication authentication) : base(handler, authentication) { }

        /// <summary>
        /// Gets the specific details about the world
        /// </summary>
        public override RestResponse OnGet(Query query)
        {
            var worlds = SystemWorld.GetCachedCelestialWorldsAsync(DbContext).Result;
            return new RestResponse(RestStatus.OK, res: worlds);
        }
    }
}
