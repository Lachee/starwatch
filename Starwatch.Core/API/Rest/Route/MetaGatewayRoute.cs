using Starwatch.API.Gateway;
using Starwatch.API.Rest.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Starwatch.API.Web;
using System.Threading.Tasks;

namespace Starwatch.API.Rest.Route
{
    [Route("/meta/gateway", AuthLevel.SuperUser)]
    class MetaGatewayRoute : RestRoute
    {
        public MetaGatewayRoute(RestHandler handler, Authentication authentication) : base(handler, authentication) { }

        public override RestResponse OnGet(Query query)
        {
            var serviceHost = Handler.HttpServer.WebSocketServices["/"];
            var gateways = Handler.ApiHandler.GetGatewayConnections();


            return new RestResponse(RestStatus.OK, res: new GatewayListing()
            {
                ActiveSessions = serviceHost.Sessions.ActiveIDs.Count(),
                InactiveSessions = serviceHost.Sessions.InactiveIDs.Count(),
                Connections = gateways.ToArray()
            });
        }


        struct GatewayListing
        {
            public int ActiveSessions { get; set; }
            public int InactiveSessions { get; set; }
            public GatewayConnection[] Connections { get; set; }
        }
    }
}
