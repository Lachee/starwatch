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
