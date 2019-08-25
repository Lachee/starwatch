using Starwatch.API.Rest.Routing;
using Starwatch.API.Web;
using System.Linq;
using Starwatch.API.Gateway.Event;
using Starwatch.API.Gateway;

namespace Starwatch.API.Rest.Route
{
    [Route("/player/all", AuthLevel.Admin)]
    class PlayerAllRoute : RestRoute, IGatewayRoute
    {
        public bool IsGateway { get; private set; }

        public PlayerAllRoute(RestHandler handler, Authentication authentication) : base(handler, authentication) { IsGateway = false; }

        public string GetRouteName() => "/player/all";

        public void SetGateway(EventConnection gateway)
        {
            this.Handler = gateway.API.RestHandler;
            this.Authentication = gateway.Authentication;
            this.IsGateway = true;
        }

        public override RestResponse OnGet(Query query)
        {
            if (!IsGateway && AuthenticationLevel < AuthLevel.SuperBot)
                return RestResponse.Forbidden;

            var players = Starbound.Connections.GetPlayersEnumerator();
            return new RestResponse(RestStatus.OK, res: players.ToArray());
        }

        
    }
}
