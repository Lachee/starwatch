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
using Starwatch.API.Rest.Routing;
using Starwatch.Entities;
using Starwatch.API.Web;

namespace Starwatch.API.Rest.Route
{
    [Route("/account/:account/authorize", AuthLevel.Bot)]
    class AccountAuthorizationRoute : RestRoute
    {
        [Argument("account")]
        public Account Account { get; set; }

        public override Type PayloadType => typeof(Payload);
        public struct Payload
        {
            public string Hash { get; set; }
        }

        public AccountAuthorizationRoute(RestHandler handler, Authentication authentication) : base(handler, authentication) { }

        /// <summary>
        /// Authorizes the account
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public override RestResponse OnPost(Query query, object payloadObject)
        {

#if !SKIP_SSL_ENFORCE
            if (!Handler.ApiHandler.IsSecure)
                return new RestResponse(RestStatus.Forbidden, msg: "Server is forbidden to handle passwords unless its using SSL.");
#endif

            Payload payload = (Payload)payloadObject;
            return new RestResponse(RestStatus.OK, BCrypt.Net.BCrypt.Verify(Account.Password, payload.Hash));
        }
    }
}
