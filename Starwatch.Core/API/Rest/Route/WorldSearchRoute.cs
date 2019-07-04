using Starwatch.API.Web;
using Starwatch.API.Rest.Routing;
using Starwatch.Entities;
using Starwatch.API.Rest.Serialization;

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

            return new RestResponse(RestStatus.OK, res: results);
        }
        
    }
}
