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
using Starwatch.Entities;
using Starwatch.API.Web;
using Starwatch.API.Rest.Serialization;
using Starwatch.Modules.Whitelist;
using System.Threading.Tasks;

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
            var task = Task.Run(async () =>
            {
                //Get the protection
                ProtectedWorld protection = await manager.GetProtectionAsync(World);
                if (protection == null) return new RestResponse(RestStatus.ResourceNotFound, msg: "Protection does not exist.");

                //Get hte account
                var result = await protection.GetAccountAsync(Account);
                return new RestResponse(RestStatus.OK, result);
            });

            return task.Result;
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
            var task = Task.Run(async () =>
            {
                //Get the protection
                ProtectedWorld protection = await manager.GetProtectionAsync(World);
                if (protection == null) return new RestResponse(RestStatus.ResourceNotFound, msg: "Protection does not exist.");

                //Get hte account
                var result = await protection.RemoveAccountAsync(Account);
                return new RestResponse(RestStatus.OK, result);
            });

            if (query.GetBool(Query.AsyncKey, false)) return RestResponse.Async;
            return task.Result;
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
            var task = Task.Run(async () =>
            {
                //Get the protection
                ProtectedWorld protection = await manager.GetProtectionAsync(World);
                if (protection == null) return new RestResponse(RestStatus.ResourceNotFound, msg: "Protection does not exist.");

                //Get hte account
                var result = await protection.SetAccountAsync(Account, query.GetString("reason", ""));
                return new RestResponse(RestStatus.OK, result);
            });

            if (query.GetBool(Query.AsyncKey, false)) return RestResponse.Async;
            return task.Result;
        }

        public WhitelistManager GetWhitelistManager()
        {
            var monitors = Starbound.GetMonitors<WhitelistManager>();
            if (monitors.Length == 0) return null;
            return monitors[0];
        }
    }
}
