using Starwatch.API.Web;
using Starwatch.API.Rest.Routing;
using Starwatch.Entities;
using Starwatch.API.Rest.Serialization;
using System;

namespace Starwatch.API.Rest.Route
{
    [Route("/world/:identifier", AuthLevel.Admin)]
    class WorldDetailsRoute : RestRoute
    {
        [Argument("identifier", Converter = typeof(WorldConverter))]
        public World World { get; set; }

        public WorldDetailsRoute(RestHandler handler, Authentication authentication) : base(handler, authentication) { }

        /// <summary>
        /// Gets the specific details about the world
        /// </summary>
        public override RestResponse OnGet(Query query)
        {
            if (World is CelestialWorld celesial && celesial.Details == null)
                try { celesial.GetDetailsAsync(Starbound).Wait(); } catch (Exception) { }

            return new RestResponse(RestStatus.OK, res: World);
        }

        /// <summary>
        /// Deletes a world metadata if it can
        /// </summary>
        public override RestResponse OnDelete(Query query)
        {
            if (World is CelestialWorld celestial)
            {
                var result = celestial.DeleteDetailsAsync(Starbound).Result;
                return new RestResponse(RestStatus.OK, res: result);
            }

            return new RestResponse(RestStatus.BadRequest, msg: "The supplied world is not a celestial world and does not contain any metadata.");
        }
    }
}
