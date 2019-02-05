using Newtonsoft.Json.Linq;
using Starwatch.API.Rest.Routing;
using Starwatch.API.Web;
using Starwatch.Starbound;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Starwatch.API.Rest.Route
{
    [Route("/server/statistics")]
    class ServerStatisticsRoute : RestRoute
    {
        public ServerStatisticsRoute(RestHandler handler, Authentication authentication) : base(handler, authentication) { }
        public override RestResponse OnGet(Query query)
        {
            var stats = Handler.Starbound.GetStatistics();
            return new RestResponse(RestStatus.OK, res: stats);
        }        
    }
}
