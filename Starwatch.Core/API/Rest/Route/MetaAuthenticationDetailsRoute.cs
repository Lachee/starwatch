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
using Starwatch.API.Rest.Routing;
using Starwatch.API.Web;
using System;

namespace Starwatch.API.Rest.Route
{
    [Route("/meta/authentication/:account", AuthLevel.Bot)]
    class MetaAuthenticationDetailsRoute : RestRoute
    {
        [Argument("account", NullValueBehaviour = ArgumentAttribute.NullBehaviour.Ignore)]
        public string AccountName { get; set; }

        public MetaAuthenticationDetailsRoute(RestHandler handler, Authentication authentication) : base(handler, authentication) { }

        class MetaAuthenticationDetailsResponse
        {
            public string Name { get; set; } = string.Empty;

            public bool IsBot { get; set; } = false;

            public bool IsUser { get; set; } = false;

            public bool IsAdmin { get; set; } = false;

            public AuthLevel AuthType { get; set; } = AuthLevel.Anonymous;

            public DateTime LastActionTime { get; set; } = DateTime.MinValue;

            public long TotalActionsPerformed { get; set; } = 0;

            public string LastAction { get; set; } = string.Empty;

            public static MetaAuthenticationDetailsResponse FromAuthentication (Authentication auth)
            {
                var response = new MetaAuthenticationDetailsResponse
                {
                    Name                  = auth.Name,
                    IsBot                 = auth.IsBot,
                    IsUser                = auth.IsUser,
                    IsAdmin               = auth.IsAdmin,
                    AuthType              = auth.AuthLevel,
                    LastActionTime        = auth.LastActionTime,
                    TotalActionsPerformed = auth.TotalActionsPerformed,
                    LastAction            = auth.LastAction
                };

                return response;
            }
        }

        public override RestResponse OnGet(Query query)
        {
            Authentication authentication;
            if (AccountName.Equals("@me"))
                authentication = Authentication;
            else
            {
                if (AuthenticationLevel < AuthLevel.SuperBot) return RestResponse.Forbidden;
                authentication = Handler.ApiHandler.GetAuthentication(AccountName);
            }

            //We could not find the authentication
            if (authentication is null)
                return RestResponse.ResourceNotFound;

            //Return the authentication
            return new RestResponse(RestStatus.OK,
                MetaAuthenticationDetailsResponse.FromAuthentication(authentication));
        }
    }
}
