using System;
using System.Collections.Generic;
using Starwatch.API.Rest.Routing;
using Starwatch.Entities;
using Starwatch.Starbound;
using Starwatch.API.Web;
using Starwatch.API;
using Starwatch.API.Rest.Serialization;
using Newtonsoft.Json.Linq;
using Starwatch.API.Rest.Route.Entities;
using Starwatch.Extensions.Whitelist;

namespace Starwatch.API.Rest.Route
{
    [Route("/world/:identifier/protection/:account", permission: AuthLevel.Admin)]
    class WorldWhitelistAccountRoute : RestRoute
    {
        [Argument("identifier", Converter = typeof(WorldConverter))]
        public World World { get; set; }

        [Argument("account", Converter = typeof(AccountConverter))]
        public Account Account { get; set; }

        public WorldWhitelistAccountRoute(RestHandler handler, Authentication authentication) : base(handler, authentication) { }

        /// <summary>
        /// Gets if the account is listed on this world
        /// </summary>
        public override RestResponse OnGet(Query query)
        {      
            //get the manager
            var manager = GetWhitelistManager();
            if (manager == null) return new RestResponse(RestStatus.ResourceNotFound, msg: "Could not find the protection manager!");

            //Get the protection
            ProtectedWorld protection = manager.GetWorld(World);
            if (protection == null) return new RestResponse(RestStatus.ResourceNotFound, msg: "Protection does not exist.");

            bool isWhitelisted = protection.AccountList.Contains(Account.Name);
            return new RestResponse(RestStatus.OK, res: isWhitelisted);
        }
        

        /// <summary>
        /// Deletes the world protection
        /// </summary>
        public override RestResponse OnDelete(Query query)
        {
            //get the manager
            var manager = GetWhitelistManager();
            if (manager == null) return new RestResponse(RestStatus.ResourceNotFound, msg: "Could not find the protection manager!");

            //Get the protection
            ProtectedWorld protection = manager.GetWorld(World);
            if (protection == null) return new RestResponse(RestStatus.ResourceNotFound, msg: "Protection does not exist.");

            //Remove the account
            bool success = manager.RemoveAccount(protection, Account);
            return new RestResponse(RestStatus.OK, res: success);

        }

        /// <summary>
        /// Adds the world protection
        /// </summary>
        public override RestResponse OnPost(Query query, object payloadObject)
        {
            //get the manager
            var manager = GetWhitelistManager();
            if (manager == null) return new RestResponse(RestStatus.ResourceNotFound, msg: "Could not find the protection manager!");

            //Get the protection
            ProtectedWorld protection = manager.GetWorld(World);
            if (protection == null) return new RestResponse(RestStatus.ResourceNotFound, msg: "Protection does not exist.");

            //Add the account
            bool success = manager.AddAccount(protection, Account);
            return new RestResponse(RestStatus.OK, res: success);
        }

        public WhitelistManager GetWhitelistManager()
        {
            var monitors = Starbound.GetMonitors<WhitelistManager>();
            if (monitors.Length == 0) return null;
            return monitors[0];
        }
    }
}
