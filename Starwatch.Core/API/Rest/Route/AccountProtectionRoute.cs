using Starwatch.API.Rest.Routing;
using Starwatch.Entities;
using Starwatch.API.Web;
using Starwatch.API.Rest.Serialization;
using Starwatch.API.Rest.Route.Entities;
using Starwatch.Modules.Whitelist;
using System.Linq;

namespace Starwatch.API.Rest.Route
{
    [Route("/account/:account/protection", permission: AuthLevel.Admin)]
    class AccountProtectionRoute : RestRoute
    {
        [Argument("account", Converter = typeof(AccountConverter))]
        public Account Account { get; set; }

        public AccountProtectionRoute(RestHandler handler, Authentication authentication) : base(handler, authentication) { }

        /// <summary>
        /// Searches for all whitelists for the account
        /// </summary>
        public override RestResponse OnGet(Query query)
        {      
            //get the manager
            var manager = GetWhitelistManager();
            if (manager == null) return new RestResponse(RestStatus.ResourceNotFound, msg: "Could not find the protection manager!");

            //Get the protection and trim the list so it doesnt include the complete account list
            var worlds = manager.GetAccountProtectionsAsync(Account).Result.Select(w => new OptionalProtectedWorld(w) { AllowAnonymous = null, AccountList = null });
            return new RestResponse(RestStatus.OK, res: worlds);

            //TODO: Reimplement This.
            //var worlds = manager.GetWorlds(Account).Select(w => new OptionalProtectedWorld(w) { AllowAnonymous = null, AccountList = null });
            //return new RestResponse(RestStatus.OK, res: worlds.ToArray());
        }
        
       
        public WhitelistManager GetWhitelistManager()
        {
            var monitors = Starbound.GetMonitors<WhitelistManager>();
            if (monitors.Length == 0) return null;
            return monitors[0];
        }
    }
}
