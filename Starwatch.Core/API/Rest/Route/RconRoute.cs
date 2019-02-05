using System;
using Starwatch.API.Rest.Routing;

using Starwatch.API.Web;
using Starwatch.Starbound.Rcon;
using System.Threading.Tasks;

namespace Starwatch.API.Rest.Route
{
    [Route("/rcon", permission: AuthLevel.SuperBot)]
    class RconRoute : RestRoute
    {
        public override Type PayloadType => typeof(Payload);
        struct Payload
        {
            public string Command { get; set; }
        }

        public RconRoute(RestHandler handler, Authentication authentication) : base(handler, authentication) { }
        public override RestResponse OnPost(Query query, object payloadObject)
        {
            Payload payload = (Payload)payloadObject;
            bool async = query.GetBool(Query.AsyncKey, false);

            if (Starbound.Rcon == null)
                return new RestResponse(RestStatus.BadRequest, "Rcon not enabled.");

            //Prepare the task
            Handler.Logger.LogWarning(Authentication + " is using RCON: " + payload.Command);
            Task<RconResponse> task = Starbound.Rcon.ExecuteAsync(payload.Command);

            //If we are async then return asap, otherwise get the result
            if (async) return RestResponse.Async;
            return new RestResponse(RestStatus.OK, res: task.Result);
        }
    }
}
