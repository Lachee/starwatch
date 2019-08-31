using Starwatch.API.Web;
using Starwatch.API.Rest.Routing;
using Starwatch.Entities;
using Starwatch.API.Rest.Serialization;
using System.Collections.Generic;

namespace Starwatch.API.Rest.Route
{
    [Route("/map", AuthLevel.User)]
    class MapRoute : RestRoute
    {
        public MapRoute(RestHandler handler, Authentication authentication) : base(handler, authentication) { }

        /// <summary>
        /// Gets the specific details about the world
        /// </summary>
        public override RestResponse OnGet(Query query)
        {
            var systems = SystemWorld.SearchCachedSystemsAsync(DbContext,
                query.GetOptionalLong("xmin"),
                query.GetOptionalLong("xmax"),
                query.GetOptionalLong("ymin"),
                query.GetOptionalLong("ymax"));

            return new RestResponse(RestStatus.OK, res: systems.Result);
        }
    }
}
