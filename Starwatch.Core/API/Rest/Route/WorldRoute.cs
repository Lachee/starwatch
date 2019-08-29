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
