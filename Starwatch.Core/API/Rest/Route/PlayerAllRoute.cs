using Starwatch.API.Rest.Routing;
using Starwatch.API.Web;
using System.Linq;
using Starwatch.API.Gateway.Event;
using Starwatch.API.Gateway;

namespace Starwatch.API.Rest.Route
{
    [Route("/player/all", AuthLevel.SuperBot)]
    class PlayerAllRoute : RestRoute, IGatewayRoute
    {
        public PlayerAllRoute(RestHandler handler, Authentication authentication) : base(handler, authentication) { }

        public string GetRouteName() => "/player/all";
        public void SetGateway(EventConnection gateway)
        {
            this.Handler = gateway.API.RestHandler;
            this.Authentication = gateway.Authentication;
        }

        public override RestResponse OnGet(Query query)
        {
            var players = Starbound.Connections.GetPlayersEnumerator();
            return new RestResponse(RestStatus.OK, res: players.ToArray());
        }

        
    }
}
