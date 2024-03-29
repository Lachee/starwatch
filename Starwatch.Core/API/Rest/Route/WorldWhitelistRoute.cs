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
using Starwatch.API.Rest.Serialization;
using Starwatch.API.Rest.Route.Entities;
using Starwatch.Modules.Whitelist;
using System.Threading.Tasks;

namespace Starwatch.API.Rest.Route
{
    [Route("/world/:identifier/protection", permission: AuthLevel.Admin)]
    class WorldWhitelistRoute : RestRoute
    {
        [Argument("identifier", Converter = typeof(WorldConverter))]
        public World World { get; set; }

        public override Type PayloadType => typeof(OptionalProtectedWorld);

        public WorldWhitelistRoute(RestHandler handler, Authentication authentication) : base(handler, authentication) { }

        /// <summary>
        /// Gets the world protection
        /// </summary>
        public override RestResponse OnGet(Query query)
        {      
            //get the manager
            var manager = GetWhitelistManager();
            if (manager == null) return new RestResponse(RestStatus.ResourceNotFound, msg: "Could not find the protection manager!");

            //Get the protection
            ProtectedWorld protection = manager.GetProtectionAsync(World).Result;
            if (protection == null) return new RestResponse(RestStatus.ResourceNotFound, msg: "The world does not have any protection.");

            //return the world
            return new RestResponse(RestStatus.OK, res: new OptionalProtectedWorld(protection));
        }
        

        /// <summary>
        /// Deletes the world protection
        /// </summary>
        public override RestResponse OnDelete(Query query)
        {       
            //get the manager
            var manager = GetWhitelistManager();

            if (manager is null) 
                return new RestResponse(RestStatus.ResourceNotFound, msg: "Could not find the protection manager!");

            var task = Task.Run(async () =>
            {
                //Get the world
                ProtectedWorld protection = await manager.GetProtectionAsync(World);

                if (protection is null) 
                    return new RestResponse(RestStatus.ResourceNotFound, msg: "The world does not have any protection.");

                //Delete the protection
                var result = await manager.RemoveProtectionAsync(protection);
                return new RestResponse(RestStatus.OK, result);
            });

            return query.GetBool(Query.AsyncKey, false) 
                ? RestResponse.Async 
                : task.Result;
        }

        /// <summary>
        /// Adds the world protection
        /// </summary>
        public override RestResponse OnPost(Query query, object payloadObject)
        {
            //get the manager
            var manager = GetWhitelistManager();
            if (manager == null) return new RestResponse(RestStatus.ResourceNotFound, msg: "Could not find the protection manager!");

            //Get the payload and make sure it svalid
            OptionalProtectedWorld patch = (OptionalProtectedWorld)payloadObject;
            if (!patch.AllowAnonymous.HasValue) return new RestResponse(RestStatus.BadRequest, msg: "Cannot create a new protection. The 'AllowAnonymous' is null or missing.");
            if (!patch.Mode.HasValue) return new RestResponse(RestStatus.BadRequest, msg: "Cannot create a new protection. The 'Mode' is null or missing.");

            var task = Task.Run(async () => {

                //Add the protection
                var protection = await manager.SetProtectionAsync(World, patch.Mode.Value, patch.AllowAnonymous.Value);

                //Add the users
                if (patch.AccountList != null)
                {
                    foreach (var accname in patch.AccountList)
                    {
                        var acc = await Starbound.Configurator.GetAccountAsync(accname);
                        await protection.SetAccountAsync(acc, "");
                    }
                }
            });

            //If we are async, return instantly
            if (query.GetBool(Query.AsyncKey, false)) return RestResponse.Async;

            //Return the newly added route
            task.Wait();
            return OnGet(query);
        }

        /// <summary>
        /// Edits the world protection
        /// </summary>
        public override RestResponse OnPatch(Query query, object payloadObject)
        {
            //get the manager
            var manager = GetWhitelistManager();
            if (manager == null) return new RestResponse(RestStatus.ResourceNotFound, msg: "Could not find the protection manager!");

            //Get the payload
            OptionalProtectedWorld patch = (OptionalProtectedWorld)payloadObject;

            //Run the patch
            var task = Task.Run(async () => {

                //Get the protection
                var protection = await manager.GetProtectionAsync(World);
                if (protection == null) return OnPost(query, payloadObject);

                //update values
                if (patch.AllowAnonymous.HasValue)
                    protection.AllowAnonymous = patch.AllowAnonymous.Value;

                if (patch.Mode.HasValue)
                    protection.Mode = patch.Mode.Value;

                if (patch.Name != null)
                    protection.Name = patch.Name;

                //Add the users
                if (patch.AccountList != null)
                {
                    foreach (var accname in patch.AccountList)
                    {
                        var acc = await Starbound.Configurator.GetAccountAsync(accname);
                        await protection.SetAccountAsync(acc, "");
                    }
                }

                await protection.SaveAsync(manager.DbContext);
                return OnGet(query);
            });

            //If we are async, return instantly
            if (query.GetBool(Query.AsyncKey, false)) return RestResponse.Async;

            //Return the newly added route
            task.Wait();
            return task.Result;


            ////get the manager
            //var manager = GetWhitelistManager();
            //if (manager == null) return new RestResponse(RestStatus.ResourceNotFound, msg: "Could not find the protection manager!");
            //
            ////Get the world
            //var protection = manager.GetWorld(World);
            //if (protection == null) return new RestResponse(RestStatus.ResourceNotFound, msg: "Could not find the protection for the world.");
            //
            ////Get the payload and make sure its valid
            //OptionalProtectedWorld patch = (OptionalProtectedWorld)payloadObject;
            //protection.Name = patch.Name ?? protection.Name;
            //protection.AllowAnonymous = patch.AllowAnonymous.GetValueOrDefault(protection.AllowAnonymous);
            //protection.Mode = patch.Mode.GetValueOrDefault(protection.Mode);
            //
            ////Add the members
            //if (patch.AccountList != null)
            //{
            //    foreach (var user in patch.AccountList)
            //    {
            //        protection.
            //    }
            //}
            //
            ////Save the new lists
            //manager.Save();
            //
            ////Return the newly patched route
            //return OnGet(query);
        }


        public WhitelistManager GetWhitelistManager()
        {
            var monitors = Starbound.GetMonitors<WhitelistManager>();
            if (monitors.Length == 0) return null;
            return monitors[0];
        }
    }
}
