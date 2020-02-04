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
        public const bool DELETE_METADATA_RESTORE = false;
        public const bool DELETE_METADATA_RESTORE_MIRROR = false;
        public const bool DELETE_METADATA_MIRROR = false;
        public const bool DELETE_METADATA_SNAPSHOT = false;

        public override int Priority => 10;
        public string RestoreDirectory { get; private set; }

        public RestoreMonitor(Server server) : base(server, "Restore Monitor")
        {
        }

        public override Task Initialize()
        {
            RestoreDirectory = Configuration.GetString("restores", "restores/");
            return Task.CompletedTask;
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
