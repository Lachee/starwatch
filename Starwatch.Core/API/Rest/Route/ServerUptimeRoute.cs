using Newtonsoft.Json.Linq;
using Starwatch.API.Rest.Routing;
using Starwatch.Starbound;
using System;
using Starwatch.API.Web;
using Starwatch.API.Gateway;
using Starwatch.API.Gateway.Event;
using Starwatch.Entities;

namespace Starwatch.API.Rest.Route
{
    [Route("/server/uptime", AuthLevel.Admin)]
    class ServerUptimeRoute : RestRoute
    {
        public ServerUptimeRoute(RestHandler handler, Authentication authentication) : base(handler, authentication) { }

        public ServerUptimeRoute() : base(null, null) { }

        public override RestResponse OnGet(Query query)
        {
            var history = Uptime.GetHistoryAsync(DbContext, query.GetInt("count", 10)).Result;
            return new RestResponse(RestStatus.OK, res: history);
        }
    }
}
