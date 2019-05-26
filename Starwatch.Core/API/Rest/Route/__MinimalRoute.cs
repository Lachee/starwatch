using Starwatch.API.Rest.Routing;

namespace Starwatch.API.Rest.Route
{
    //[Route("/example/minium")]
    class __MinimalRoute : RestRoute
    {
        public __MinimalRoute(RestHandler handler, Authentication authentication) : base(handler, authentication) { }
    }
}
