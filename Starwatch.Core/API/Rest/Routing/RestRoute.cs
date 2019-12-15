/*
START LICENSE DISCLAIMER
Starwatch is a Starbound Server manager with player management, crash recovery and a REST and websocket (live) API. 
Copyright(C) 2020 Lachee

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published
by the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with this program. If not, see < https://www.gnu.org/licenses/ >.
END LICENSE DISCLAIMER
*/
using System;
using Starwatch.API.Web;
using Starwatch.Database;

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
        /// The database context
        /// </summary>
        public DbContext DbContext => Handler.ApiHandler.DbContext;

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
