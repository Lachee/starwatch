using System;
using Starwatch.API.Rest.Routing;
using Starwatch.Entities;
using Starwatch.API.Rest.Route.Entities;
using Starwatch.API.Rest.Serialization;
using Starwatch.API.Web;
using Starwatch.API.Gateway.Event;
using Starwatch.API.Gateway;
using System.Linq;
using System.Threading.Tasks;

namespace Starwatch.API.Rest.Route
{
    [Route("/account/:account", AuthLevel.Bot)]
    class AccountDetailsRoute : RestRoute, IGatewayRoute
    {
        [Argument("account", Converter = typeof(AccountConverter))]
        public Account Account { get; set; }

        public override Type PayloadType => typeof(AccountPatch);


        public AccountDetailsRoute(RestHandler handler, Authentication authentication) : base(handler, authentication) { }
        public AccountDetailsRoute(Account account) : base(null, null) { Account = account; }

        public string GetRouteName() => "/account/:account";
        public void SetGateway(EventConnection gateway)
        {
            Handler = gateway.API.RestHandler;
            Authentication = gateway.Authentication;
        }

        /// <summary>
        /// Gets the account exists and its admin state.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public override RestResponse OnGet(Query query)
        {
            if (AuthenticationLevel == AuthLevel.SuperUser) return new RestResponse(RestStatus.OK, new Account(Account));
            return new RestResponse(RestStatus.OK, new Account(Account) { Password = null });
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
            
            //Reload the server
            var task = Task.Run(async () =>
            {
                if (!await Starbound.Configurator.RemoveAccountAsync(Account))
                    return false;

                return await Starbound.SaveConfigurationAsync(true);
            });

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

#if !SKIP_SSL_ENFORCE
            if (oa.Password != null && !Handler.ApiHandler.IsSecure)
                return new RestResponse(RestStatus.Forbidden, msg: "Cannot set passwords if the server is not SSL");
#endif

            string username = oa.Name ?? Account.Name;
            string password = oa.Password ?? Account.Password;
            bool isAdmin = oa.IsAdmin ?? Account.IsAdmin;
            bool isActive = oa.IsActive ?? Account.IsActive;

            //Prepare the task
            var task = Task.Run(async () =>
            {
                if (username != Account.Name)
                {
                    //Make sure the name isnt a duplicate
                    if (Starbound.Configurator.GetAccountAsync(username) != null)
                        return new RestResponse(RestStatus.BadRequest, "The username " + username + " already exists.");

                    //Remove the old account
                    await Starbound.Configurator.RemoveAccountAsync(Account);

                    //Create the new account
                    Account = new Account(username)
                    {
                        IsAdmin = isAdmin,
                        Password = password,
                        IsActive = isActive
                    };

                }
                else
                {
                    //Edit the individual parts of the account
                    Account.Password = password;
                    Account.IsAdmin = isAdmin;
                    Account.IsActive = isActive;
                }

                //Set the account                
                await Starbound.Configurator.SetAccountAsync(Account);

                //Terminate any connections
                var auth = this.Handler.ApiHandler.GetAuthentication(Account.Name);
                this.Handler.ApiHandler.DisconnectAuthentication(auth, reason: "Authentication change");

                //Logout anyone that previously connected
                foreach (var player in Starbound?.Connections?.GetCopiedPlayersEnumerable().Where(p => p != null && p.AccountName != null && p.AccountName.Equals(Account.Name)))
                    await player.Kick("Account details changed.");

                //Apply the settings
                await Starbound.SaveConfigurationAsync(true);
                return OnGet(query);
            });

            //If we are async, abort asap, otherwise wait for it to finish.
            if (query.GetBool(Query.AsyncKey, false)) return RestResponse.Async;
            return task.Result;
        }
    }
}
