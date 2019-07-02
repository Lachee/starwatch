using Starwatch.API.Rest.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using Starwatch.API.Web;
using System.Text;
using Starwatch.Starbound;

namespace Starwatch.API.Rest.Route
{
    [Route("/session", AuthLevel.Admin)]
    class SessionRoute : RestRoute
    {
        public SessionRoute(RestHandler handler, Authentication authentication) : base(handler, authentication) { }
        public override RestResponse OnGet(Query query)
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            if (query.TryGetValue("username", out var username))
            {
                parameters.Add("username", username);
                parameters.Add("username_clean", username);
            }

            if (query.TryGetValue("account", out var account))
                parameters.Add("account", account);

            if (query.TryGetValue("ip", out var ip))
                parameters.Add("ip", ip);

            if (query.TryGetValue("uuid", out var uuid))
                parameters.Add("uuid", uuid);

            if (query.TryGetValue("connection", out var connection))
                parameters.Add("cid", connection);

            TimeSpan? timespan = null;
            if (query.TryGetInt("seconds", out var second)) timespan = TimeSpan.FromSeconds(second);
            if (query.TryGetInt("minutes", out var minutes)) timespan = TimeSpan.FromMinutes(minutes);
            if (query.TryGetInt("hours", out var hours)) timespan = TimeSpan.FromHours(hours);
            if (query.TryGetInt("days", out var days)) timespan = TimeSpan.FromDays(days);

            var result = Session.FindAsync(Starbound.DbContext, Starbound, parameters, mode: "AND", duration: timespan).Result;
            return new RestResponse(RestStatus.OK, result);
        }
        
    }
}
