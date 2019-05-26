using Newtonsoft.Json.Linq;
using Starwatch.API.Gateway;
using Starwatch.API.Gateway.Event;
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
    class ServerStatisticsRoute : RestRoute, IGatewayRoute
    {
        public ServerStatisticsRoute(RestHandler handler, Authentication authentication) : base(handler, authentication) { }
        public ServerStatisticsRoute() { }

        public string GetRouteName() => "/server/statistics";
        public void SetGateway(EventConnection gateway)
        {
            Handler = gateway.API.RestHandler;
            Authentication = gateway.Authentication;
        }

        public override RestResponse OnGet(Query query)
        {
            var stats = Handler.Starbound.GetStatistics();
            return new RestResponse(RestStatus.OK, res: stats);
        }


    }
}
