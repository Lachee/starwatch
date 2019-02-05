using System;
using System.Collections.Generic;
using Starwatch.API.Rest.Routing;
using Starwatch.Entities;
using Starwatch.Starbound;
using Starwatch.API;

using Starwatch.API.Web;

namespace Starwatch.API.Rest.Route
{
    //[Route("/example/minium")]
    class __MinimalRoute : RestRoute
    {
        public __MinimalRoute(RestHandler handler, Authentication authentication) : base(handler, authentication) { }
    }
}
