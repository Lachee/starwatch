using System;
using System.Collections.Generic;
using Starwatch.API.Rest.Routing;
using Starwatch.Entities;
using Starwatch.API.Web;
using Starwatch.Starbound;
using Starwatch.API;
using Starwatch.API.Rest.Serialization;
using Newtonsoft.Json.Linq;
using Starwatch.API.Rest.Route.Entities;
using Starwatch.Extensions.Whitelist;

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
            ProtectedWorld protection = manager.GetWorld(World);
            if (protection == null) return new RestResponse(RestStatus.ResourceNotFound, msg: "Protection does not exist.");

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
            if (manager == null) return new RestResponse(RestStatus.ResourceNotFound, msg: "Could not find the protection manager!");

            //Delete the protection
            if (!manager.RemoveWorld(World))
                return new RestResponse(RestStatus.ResourceNotFound, msg: "World does not have protection.");

            //Save the new lists
            manager.Save();

            return new RestResponse(RestStatus.OK, res: true);

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

            //Add the protection
            if (!manager.AddWorld(World, patch.Mode.Value, patch.AllowAnonymous.Value, patch.Name))
                return new RestResponse(RestStatus.BadRequest, msg: "Failed to create protection. Please ensure the protection doesnt already exist.");
            
            //Add the members
            if (patch.AccountList != null)
                manager.AddAccounts(World, patch.AccountList);

            //Save the new lists
            manager.Save();

            //Return the newly added route
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

            //Get the world
            var protection = manager.GetWorld(World);
            if (protection == null) return new RestResponse(RestStatus.ResourceNotFound, msg: "Could not find the protection for the world.");

            //Get the payload and make sure its valid
            OptionalProtectedWorld patch = (OptionalProtectedWorld)payloadObject;
            protection.Name = patch.Name ?? protection.Name;
            protection.AllowAnonymous = patch.AllowAnonymous.GetValueOrDefault(protection.AllowAnonymous);
            protection.Mode = patch.Mode.GetValueOrDefault(protection.Mode);

            //Add the members
            if (patch.AccountList != null)
                manager.AddAccounts(World, patch.AccountList);

            //Save the new lists
            manager.Save();

            //Return the newly patched route
            return OnGet(query);
        }


        public WhitelistManager GetWhitelistManager()
        {
            var monitors = Starbound.GetMonitors<WhitelistManager>();
            if (monitors.Length == 0) return null;
            return monitors[0];
        }
    }
}
