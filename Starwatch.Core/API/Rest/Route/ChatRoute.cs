using System;
using System.Collections.Generic;
using Starwatch.API.Rest.Routing;
using Starwatch.Entities;
using Starwatch.Starbound;
using Starwatch.API;

using Starwatch.API.Web;

namespace Starwatch.API.Rest.Route
{
    [Route("/chat", AuthLevel.Admin)]
    class ChatRoute : RestRoute
    {
        public override Type PayloadType => typeof(Payload);
        struct Payload
        {
            public string Content { get; set; }
        }

        public ChatRoute(RestHandler handler, Authentication authentication) : base(handler, authentication) { }

        public override RestResponse OnPost(Query query, object payloadObject)
        {
            if (Starbound.Rcon == null)
                return new RestResponse(RestStatus.BadRequest, "Rcon not enabled.");

            var payload = (Payload)payloadObject;
            string message = payload.Content;

            if (query.GetBool("include_tag", false))
            {
                message = $"<^pink;{Authentication.Name}^reset;> {message}";
            }

            var task = Starbound.Rcon.BroadcastAsync(message);
            if (query.GetBool(Query.AsyncKey, false)) return RestResponse.Async;
            return new RestResponse(RestStatus.OK, res: task.Result);
        }
    }
}
