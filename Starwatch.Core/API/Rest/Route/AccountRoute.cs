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
using Starwatch.API.Rest.Route.Entities;
using Starwatch.API.Web;
using System.Threading.Tasks;

namespace Starwatch.API.Rest.Route
{
    [Route("/account", AuthLevel.Bot)]
    class AccountRoute : RestRoute
    {
        public override Type PayloadType => typeof(AccountPatch);
        public AccountRoute(RestHandler handler, Authentication authentication) : base(handler, authentication) { }

        /// <summary>
        /// Creates an account.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="payloadObject"></param>
        /// <returns></returns>
        public override RestResponse OnPost(Query query, object payloadObject)
        {
#if !SKIP_SSL_ENFORCE
            //Server must be in SSL mode
            if (!Handler.ApiHandler.IsSecure)
                return new RestResponse(RestStatus.Forbidden, msg: "Cannot set passwords if the server is not SSL");
#endif

            //Validate the account
            AccountPatch account = (AccountPatch)payloadObject;
            if (string.IsNullOrWhiteSpace(account.Name)) return new RestResponse(RestStatus.BadRequest, msg: "Account name cannot be empty");
            if (string.IsNullOrWhiteSpace(account.Password)) return new RestResponse(RestStatus.BadRequest, msg: "Password cannot be empty");

            //Only SuperBot and SuperUsers can create admin
            if (account.IsAdmin.GetValueOrDefault(false) && AuthenticationLevel < AuthLevel.SuperBot)
                return new RestResponse(RestStatus.Forbidden, "Only SuperBot or above may create admin accounts.");

            var task = Task.Run(async () =>
            {

                //Make sure the name isnt a duplicate
                if (await Starbound.Configurator.GetAccountAsync(account.Name) != null)
                    return new RestResponse(RestStatus.BadRequest, $"The username {account.Name} already exists.");
            
                //Post it
                await Starbound.Configurator.SetAccountAsync(account.ToAccount());

                //Save the settings
                await Starbound.SaveConfigurationAsync(true);
                return new RestResponse(RestStatus.OK, msg: $"Saved: " + account.Name, res: account);
            });

            if (query.GetBool(Query.AsyncKey, false)) return RestResponse.Async;
            return task.Result;
        }
    }
}
