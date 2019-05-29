using System;
using Starwatch.API.Web;

namespace Starwatch.API.Rest.Routing
{
    public abstract class RestRoute
    {
        /// <summary>
        /// The main handler of the request
        /// </summary>
        public RestHandler Handler { get; internal set; }

        /// <summary>
        /// The current starbound server.
        /// </summary>
        public Starbound.Server Starbound => Handler.Starbound;

        /// <summary>
        /// The authentication that is executing the endpoint
        /// </summary>
        public Authentication Authentication { get; internal set; }

        /// <summary>
        /// The current permission level the authentication has.
        /// </summary>
        public AuthLevel AuthenticationLevel => Authentication.AuthLevel;

        /// <summary>
        /// The payload type for the POST and PUT requests.
        /// </summary>
        public virtual Type PayloadType => null;

        /// <summary>
        /// Is the endpoint silent and not log?
        /// </summary>
        public virtual bool Silent => false;

        public RestRoute() { }
        public RestRoute(RestHandler handler, Authentication authentication)
        {
            Handler = handler;
            Authentication = authentication;
        }

        public virtual RestResponse OnGet(Query query) => RestResponse.BadMethod;
        public virtual RestResponse OnDelete(Query query) => RestResponse.BadMethod;
        public virtual RestResponse OnPost(Query query, object payloadObject) => RestResponse.BadMethod;
        public virtual RestResponse OnPatch(Query query, object payloadObject) => RestResponse.BadMethod;

    }
}
