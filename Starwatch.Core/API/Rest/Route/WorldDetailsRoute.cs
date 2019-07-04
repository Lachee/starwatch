using Starwatch.API.Web;
using Starwatch.API.Rest.Routing;
using Starwatch.Entities;
using Starwatch.API.Rest.Serialization;

namespace Starwatch.API.Rest.Route
{
    [Route("/world/:identifier")]
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
                celesial.LoadDetailsAsync(Starbound).Wait();

            return new RestResponse(RestStatus.OK, res: World);
        }

        /// <summary>
        /// Moves the world file to another file.
        /// </summary>
        public override RestResponse OnPatch(Query query, object payloadObject)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Deletes the world file.
        /// </summary>
        public override RestResponse OnDelete(Query query)
        {
            throw new System.NotImplementedException();
        }
    }
}
