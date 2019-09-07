using System;
using Starwatch.API.Rest.Routing;
using Starwatch.API.Web;
using Starwatch.Entities;
using Starwatch.API.Rest.Serialization;
using Starwatch.API.Rest.Route.Entities;
using Starwatch.Modules.Restore;

namespace Starwatch.API.Rest.Route
{
    [Route("/world/:whereami/restore", permission: AuthLevel.Admin)]
    class WorldBackupRoute : RestRoute
    {
        [Argument("whereami", Converter = typeof(WorldConverter))]
        public World World { get; set; }

        public override Type PayloadType => typeof(WorldRestorePatch);
        public class WorldRestorePatch  {  public string Mirror { get; set; } }


        public WorldBackupRoute(RestHandler handler, Authentication authentication) : base(handler, authentication) { }

        /// <summary>
        /// Gets the world backup
        /// </summary>
        public override RestResponse OnGet(Query query)
        {
            //get the manager
            RestoreMonitor restorer = GetRestoreMonitor();
            if (restorer == null) return new RestResponse(RestStatus.ResourceNotFound, msg: "Could not find the restore manager!");

            //Get the backup
            var restore = restorer.GetWorldRestoreAsync(World).Result;
            if (restore == null) return new RestResponse(RestStatus.ResourceNotFound, msg: "Restore does not exist.");

            //return the world
            return new RestResponse(RestStatus.OK, res: restore);
        }

        /// <summary>
        /// Forces a update
        /// </summary>
        public override RestResponse OnPost(Query query, object payloadObject)
        {
            //get the manager
            RestoreMonitor restorer = GetRestoreMonitor();
            if (restorer == null) return new RestResponse(RestStatus.ResourceNotFound, msg: "Could not find the restore manager!");

            //Get the backup
            var restore = restorer.CreateSnapshotAsync(World).Result;
            if (restore == null) return new RestResponse(RestStatus.ResourceNotFound, msg: "Restore does not exist.");

            return new RestResponse(RestStatus.OK, res: restore); 
        }

        /// <summary>
        /// Deletes all the backups
        /// </summary>
        public override RestResponse OnDelete(Query query)
        {
            //get the manager
            RestoreMonitor restorer = GetRestoreMonitor();
            if (restorer == null) return new RestResponse(RestStatus.ResourceNotFound, msg: "Could not find the restore manager!");

            //Get the backup
            var task = restorer.DeleteRestoreAsync(World);
            if (query.IsAsync) return RestResponse.Async;

            //Delete the backup
            return new RestResponse(RestStatus.OK, res: task.Result);
        }

        /// <summary>
        /// Edits the world backup
        /// </summary>
        public override RestResponse OnPatch(Query query, object payloadObject)
        {
            //get the patch
            WorldRestorePatch patch = (WorldRestorePatch)payloadObject;
            
            //get the manager
            RestoreMonitor restorer = GetRestoreMonitor();
            if (restorer == null) return new RestResponse(RestStatus.ResourceNotFound, msg: "Could not find the restore manager!");

            var result = restorer.SetMirrorAsync(World, patch.Mirror != null ? World.Parse(patch.Mirror) : null).Result;
            if (!result) return new RestResponse(RestStatus.ResourceNotFound, msg: "Server failed to patch the world. Does it exist?");

            return OnGet(query);
        }


        public RestoreMonitor GetRestoreMonitor()
        {
            var monitors = Starbound.GetMonitors<RestoreMonitor>();
            if (monitors.Length == 0) return null;
            return monitors[0];
        }
    }
}
