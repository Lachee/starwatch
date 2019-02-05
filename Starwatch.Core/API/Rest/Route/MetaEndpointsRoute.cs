using Starwatch.API.Rest.Routing;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Starwatch.API.Web;

namespace Starwatch.API.Rest.Route
{
    [Route("/meta/endpoints", AuthLevel.Bot)]
    class MetaEndpointsRoute : RestRoute
    {
        public MetaEndpointsRoute(RestHandler handler, Authentication authentication) : base(handler, authentication) { }

        public override RestResponse OnGet(Query query)
        {
            return new RestResponse(RestStatus.OK, res: Handler.GetRoutes());
        }
    }
}
