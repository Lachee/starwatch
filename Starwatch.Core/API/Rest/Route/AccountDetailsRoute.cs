using System;
using System.Collections.Generic;
using Starwatch.API.Rest.Routing;
using Starwatch.Entities;
using Starwatch.Starbound;
using Starwatch.API;
using Newtonsoft.Json;
using Starwatch.API.Rest.Route.Entities;
using Starwatch.API.Rest.Serialization;
using Starwatch.API.Web;

namespace Starwatch.API.Rest.Route
{
    [Route("/account/:account", AuthLevel.Bot)]
    class AccountDetailsRoute : RestRoute
    {
        [Argument("account", Converter = typeof(AccountConverter))]
        public Account Account { get; set; }

        public override Type PayloadType => typeof(AccountPatch);


        public AccountDetailsRoute(RestHandler handler, Authentication authentication) : base(handler, authentication) { }

        /// <summary>
        /// Gets the account exists and its admin state.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public override RestResponse OnGet(Query query)
        {
            if (AuthenticationLevel == AuthLevel.SuperUser) return new RestResponse(RestStatus.OK, new AccountPatch(Account));
            return new RestResponse(RestStatus.OK, new AccountPatch(Account) { Password = null });
        }

        /// <summary>
        /// Deletes the account
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public override RestResponse OnDelete(Query query)
        {
            //Make sure we have permission to edit admin accounts
            if (Account.IsAdmin && AuthenticationLevel < AuthLevel.SuperBot)
                return new RestResponse(RestStatus.Forbidden, msg: "Only SuperBots or above may delete admin accounts");

            //Delete the account, return false if unsuccesful
            if (!Starbound.Settings.Accounts.RemoveAccount(Account))
                return new RestResponse(RestStatus.OK, res: false);

            //Reload the server
            var task = Starbound.SaveSettings(true);
            if (query.GetBool(Query.AsyncKey, false)) return RestResponse.Async;
            return new RestResponse(RestStatus.OK, res: task.Result);
        }
        
        /// <summary>
        /// Updates the account
        /// </summary>
        /// <param name="query"></param>
        /// <param name="payloadObject"></param>
        /// <returns></returns>
        public override RestResponse OnPatch(Query query, object payloadObject)
        {
            AccountPatch oa = (AccountPatch)payloadObject;

            if (oa.IsAdmin.HasValue && AuthenticationLevel < AuthLevel.SuperBot)
                return new RestResponse(RestStatus.Forbidden, msg: "Only SuperBots or above may patch admin accounts");

            if (RestHandler.ENFORCE_SSL_PASSWORDS && oa.Password != null && !Handler.ApiHandler.IsSecure)
                return new RestResponse(RestStatus.Forbidden, msg: "Cannot set passwords if the server is not SSL");

            string username = oa.Name ?? Account.Name;
            string password = oa.Password ?? Account.Password;
            bool isAdmin = oa.IsAdmin ?? Account.IsAdmin;
            
            if (username != Account.Name)
            {
                //Make sure the name isnt a duplicate
                if (Starbound.Settings.Accounts.GetAccount(username) != null)
                    return new RestResponse(RestStatus.BadRequest, "The username " + username + " already exists.");
                
                //Remove the old account
                Starbound.Settings.Accounts.RemoveAccount(Account);

                //Create the new account
                Account = new Account(username)
                {
                    IsAdmin = isAdmin,
                    Password = password
                };

                //Post it
                Starbound.Settings.Accounts.AddAccount(Account);
            }
            else
            {
                //Edit the individual parts of the account
                Account.Password = password;
                Account.IsAdmin = isAdmin;
            }

            //Save the settings
            var task = Starbound.SaveSettings(true);
            if (query.GetBool(Query.AsyncKey, false)) return RestResponse.Async;

            //Wait for the settings to finish saving, then return the new account
            task.Wait();
            return OnGet(query);
        }
    }
}
