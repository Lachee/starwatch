using System;
using Starwatch.API.Rest.Routing;
using Starwatch.API.Web;
using Starwatch.Entities;
using Starwatch.API.Rest.Serialization;
using Starwatch.Extensions.Backup;
using Starwatch.API.Rest.Route.Entities;

namespace Starwatch.API.Rest.Route
{
    [Route("/world/:identifier/backup", permission: AuthLevel.Admin)]
    class WorldBackupRoute : RestRoute
    {
        [Argument("identifier", Converter = typeof(WorldConverter))]
        public World World { get; set; }

        public override Type PayloadType => typeof(OptionalWorldBackup);

        public WorldBackupRoute(RestHandler handler, Authentication authentication) : base(handler, authentication) { }

        /// <summary>
        /// Gets the world backup
        /// </summary>
        public override RestResponse OnGet(Query query)
        {
            //get the manager
            BackupManager manager = GetBackupManager();
            if (manager == null) return new RestResponse(RestStatus.ResourceNotFound, msg: "Could not find the backup manager!");

            //Get the backup
            WorldBackup backup = manager.GetBackup(World);
            if (backup == null) return new RestResponse(RestStatus.ResourceNotFound, msg: "Backup does not exist.");

            //return the world
            return new RestResponse(RestStatus.OK, res: new OptionalWorldBackup(backup));
        }

        /// <summary>
        /// Forces a update
        /// </summary>
        public override RestResponse OnPost(Query query, object payloadObject)
        {
            //get the manager
            BackupManager manager = GetBackupManager();
            if (manager == null) return new RestResponse(RestStatus.ResourceNotFound, msg: "Could not find the backup manager!");

            //Get the backup
            WorldBackup backup = manager.GetBackup(World);
            if (backup == null) return new RestResponse(RestStatus.ResourceNotFound, msg: "Backup does not exist.");

            //Force the backup and then return the object
            backup.Backup();
            return OnGet(query);
        }

        /// <summary>
        /// Deletes all the backups
        /// </summary>
        public override RestResponse OnDelete(Query query)
        {  
            //get the manager
            BackupManager manager = GetBackupManager();
            if (manager == null) return new RestResponse(RestStatus.ResourceNotFound, msg: "Could not find the backup manager!");
            
            //Make sure the query is set
            bool deleteFiles;
            if (!query.TryGetBool("delete", out deleteFiles))
                return new RestResponse(RestStatus.BadRequest, msg: "The query 'delete' was not defined or was not a valid bool.");

            //Make sure its all G
            if (deleteFiles && AuthenticationLevel < AuthLevel.Bot)
                return new RestResponse(RestStatus.Forbidden, msg: "Non-Bot users are forbidden from deleting files!");
            
            //Delete the backup
            var result = manager.Remove(World, deleteFiles);
            return new RestResponse(RestStatus.OK, res: result);
        }

        /// <summary>
        /// Edits the world backup
        /// </summary>
        public override RestResponse OnPatch(Query query, object payloadObject)
        {
            //get the patch
            OptionalWorldBackup patch = (OptionalWorldBackup)payloadObject;
            if (patch.LastBackup.HasValue) return new RestResponse(RestStatus.BadRequest, msg: "LastBackup cannot be set with a patch.");

            //get the manager
            BackupManager manager = GetBackupManager();
            if (manager == null) return new RestResponse(RestStatus.ResourceNotFound, msg: "Could not find the backup manager!");

            //Get the backup
            WorldBackup backup = manager.GetBackup(World);
            if (backup != null)
            {
                //Get what we should be setting
                bool isAutoRestore = patch.IsAutoRestore.GetValueOrDefault(backup.IsAutoRestore);
                bool isRolling = patch.IsRolling.GetValueOrDefault(backup.IsRolling);

                //Remove the old one and add the new one
                manager.Remove(World, deleteFiles: false);
                manager.Add(World, isAutoRestore, isRolling);
            }
            else
            {
                //Validate everything is completed.
                if (!patch.IsAutoRestore.HasValue) return new RestResponse(RestStatus.BadRequest, msg: "A new backup has to be created but the IsAutoRestore is not set.");
                if (!patch.IsRolling.HasValue) return new RestResponse(RestStatus.BadRequest, msg: "A new backup has to be created but the IsRolling is not set.");

                //Add the item
                manager.Add(World, patch.IsAutoRestore.Value, patch.IsRolling.Value);
            }


            //Return the new backup
            return this.OnGet(query);
        }


        public BackupManager GetBackupManager()
        {
            var monitors = Starbound.GetMonitors<BackupManager>();
            if (monitors.Length == 0) return null;
            return monitors[0];
        }
    }
}
