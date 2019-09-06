using Starwatch.Entities;
using Starwatch.Monitoring;
using Starwatch.Starbound;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starwatch.Modules.Restore
{
    public class RestoreMonitor : ConfigurableMonitor
    {
        public const bool DELETE_METADATA_RESTORE           = false;
        public const bool DELETE_METADATA_RESTORE_MIRROR    = false;
        public const bool DELETE_METADATA_MIRROR            = false;
        public const bool DELETE_METADATA_SNAPSHOT          = false;

        public override int Priority => 10;
        public string RestoreDirectory
        {
            get => Configuration.GetString("restores", "restores/");
            set => Configuration.SetKey("restores", value);
        }

        public RestoreMonitor(Server server) : base(server, "Restore Monitor")
        {
        }

        public override async Task OnServerPreStart()
        {
            Logger.Log("Restoring all the worlds...");
            await RestoreAllAsync();
        }

        /// <summary>
        /// Sets a world to mirror another world. Returns false if the world doesn't exist
        /// </summary>
        /// <param name="world"></param>
        /// <param name="mirror"></param>
        /// <returns></returns>
        public async Task<bool> SetMirrorAsync(World world, World mirror)
        {
            //Get the restore or create a new one
            WorldRestore restore = await GetWorldRestoreAsync(world);
            if (restore == null) return false;

            restore.Mirror = mirror;
            await restore.SaveAsync(DbContext);

            if (DELETE_METADATA_MIRROR && world is CelestialWorld celestial)
                await celestial.DeleteDetailsAsync(Server, DbContext);

            return true;
        }

        /// <summary>
        /// Creates a new snapshot. If the world hasn't been added to the list yet, it will be added
        /// </summary>
        /// <returns></returns>
        public async Task<WorldRestore> CreateSnapshotAsync(World world)
        {
            //Get the restore or create a new one
            WorldRestore restore = await GetWorldRestoreAsync(world);
            if (restore == null) restore = new WorldRestore(this, world);

            restore.Snapshot();
            await restore.SaveAsync(DbContext);

            if (DELETE_METADATA_SNAPSHOT && world is CelestialWorld celestial)
                await celestial.DeleteDetailsAsync(Server, DbContext);

            return restore;
        }

        /// <summary>
        /// Deletes a restore
        /// </summary>
        /// <param name="world"></param>
        /// <param name="deleteFile"></param>
        /// <returns></returns>
        public async Task<bool> DeleteRestoreAsync(World world)
        {
            WorldRestore restore = await GetWorldRestoreAsync(world);
            if (restore == null) return false;
            return await restore.DeleteAsync(DbContext);
        }


        public async Task<WorldRestore> GetWorldRestoreAsync(World world)
        {
            WorldRestore restore = new WorldRestore(this, world);
            if (!await restore.LoadAsync(DbContext)) return null;
            return restore;
        }

        /// <summary>
        /// Restores all the worlds.
        /// </summary>
        /// <returns></returns>
        public async Task RestoreAllAsync()
        {
            var worlds = await WorldRestore.LoadAllAsync(DbContext, this);
            foreach(var w in worlds.OrderBy(w => w.Priority))
            {
                Logger.Log("Restoring World: " + w.World);
                if (!await w.RestoreAsync())
                    Logger.LogWarning("Failed to restore the world.");
            }
        }
    }
}
