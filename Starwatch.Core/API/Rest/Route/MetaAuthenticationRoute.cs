using Starwatch.API.Rest.Routing;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Starwatch.API.Web;

namespace Starwatch.API.Rest.Route
{
    [Route("/meta/authentication", AuthLevel.SuperUser)]
    class MetaAuthenticationRoute : RestRoute
    {
        public MetaAuthenticationRoute(RestHandler handler, Authentication authentication) : base(handler, authentication) { }

        public override RestResponse OnGet(Query query)
        {
            string[] authenticationNames = Handler.ApiHandler.GetAuthenticationNames();
            return new RestResponse(RestStatus.OK, res: authenticationNames);
        }

        public override RestResponse OnDelete(Query query)
        {
            Handler.ApiHandler.ClearAuthentications();
            return new RestResponse(RestStatus.OK);
        }
    }
}
