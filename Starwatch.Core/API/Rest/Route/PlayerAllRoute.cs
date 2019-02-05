using System;
using System.Collections.Generic;
using Starwatch.API.Rest.Routing;
using Starwatch.API.Web;
using Starwatch.Entities;
using Starwatch.Starbound;
using Starwatch.API;
using Starwatch.API.Rest.Serialization;
using System.Linq;

namespace Starwatch.API.Rest.Route
{
    [Route("/player/all", AuthLevel.SuperBot)]
    class PlayerAllRoute : RestRoute
    {
        public PlayerAllRoute(RestHandler handler, Authentication authentication) : base(handler, authentication) { }
        public override RestResponse OnGet(Query query)
        {
            var players = Starbound.Connections.GetPlayersEnumerator();
            return new RestResponse(RestStatus.OK, res: players.ToArray());
        }
    }
}
