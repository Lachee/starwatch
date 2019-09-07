using Newtonsoft.Json;
using Starwatch.Database;
using Starwatch.Entities;
using Starwatch.Starbound;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Starwatch.Modules.Restore
{
    public class WorldRestore : IRecord
    {
        const string TABLE = "!restores";
        public string Table => TABLE;

        /// <summary>
        /// The restore monitor
        /// </summary>
        [JsonIgnore] public RestoreMonitor Monitor { get; }

        /// <summary>The world to parse</summary>
        [JsonIgnore] public World World { get; set; }

        /// <summary>The world to mirror</summary>
        [JsonIgnore] public World Mirror { get; set; }

        /// <summary>
        /// The order to restore the world.
        /// </summary>
        public int Priority => Mirror != null ? 20 : 10;

        [JsonProperty("World")]
        private string WorldWhereami { get => World?.Whereami; set => World = World.ParseWhereami(value); }

        [JsonProperty("Mirror")]
        public string MirrorWhereami { get => Mirror?.Whereami; set => Mirror = value == null ? null : World.ParseWhereami(value); }

        /// <summary>
        /// Creates a new world restore monitor
        /// </summary>
        /// <param name="monitor"></param>
        public WorldRestore(RestoreMonitor monitor, World world)
        {
            Monitor = monitor;
            World = world;
        }

        /// <summary>
        /// Creates a new restore image.
        /// </summary>
        /// <param name="server"></param>
        /// <param name="restoreDirectory"></param>
        public void Snapshot()
        {
            if (!Directory.Exists(Monitor.RestoreDirectory))
                Directory.CreateDirectory(Monitor.RestoreDirectory);

            string destination = Path.Combine(Monitor.RestoreDirectory, World.Filename);
            string origin = World.GetAbsolutePath(Monitor.Server);
            File.Copy(origin, destination, true);
        }

        /// <summary>
        /// Restores the world
        /// </summary>
        /// <param name="server"></param>
        /// <param name="restoreDirectory"></param>
        /// <returns></returns>
        public async Task<bool> RestoreAsync()
        {
            string origin = Path.Combine(Monitor.RestoreDirectory, World.Filename);
            string destination = World.GetAbsolutePath(Monitor.Server);

            if (Mirror != null)
            {
                //Clone the world
                origin = Mirror.GetAbsolutePath(Monitor.Server);
            }

            //Make sure the file exists
            if (!File.Exists(origin)) return false;

            //Copy the file
            File.Copy(origin, destination, true);

            //Should we delete our metadata
            if (RestoreMonitor.DELETE_METADATA_RESTORE || (Mirror != null && RestoreMonitor.DELETE_METADATA_RESTORE_MIRROR))
            {
                if (World is CelestialWorld celestial)
                    await celestial.DeleteDetailsAsync(Monitor.Server, Monitor.DbContext);
            }

            return true;
        }

        /// <summary>
        /// Deletes the restore file
        /// </summary>
        /// <param name="restoreDirectory"></param>
        /// <returns></returns>
        public async Task<bool> DeleteAsync(DbContext db)
        {
            //Get the file and delete it if we can
            string origin = Path.Combine(Monitor.RestoreDirectory, World.Filename);
            if (File.Exists(origin)) File.Delete(origin);

            //Delete the SQL entry
            return await db.DeleteAsync(TABLE, new Dictionary<string, object>() { ["world"] = WorldWhereami });
        }


        /// <summary>
        /// Loads all the world restores
        /// </summary>
        /// <param name="db"></param>
        /// <returns></returns>
        public static Task<List<WorldRestore>> LoadAllAsync(DbContext db, RestoreMonitor monitor) => db.SelectAsync(TABLE, (r) => FromDBReader(r, monitor), order: "priority");
        public async Task<bool> LoadAsync(DbContext db)
        {
            WorldRestore restore = await db.SelectOneAsync<WorldRestore>(TABLE, (r) => FromDBReader(r, Monitor), new Dictionary<string, object>() { ["world"] = WorldWhereami });
            if (restore == null) return false;

            WorldWhereami = restore.WorldWhereami;
            MirrorWhereami = restore.MirrorWhereami;
            //Priority = restore.Priority;
            return true;
        }

        private static WorldRestore FromDBReader(DbDataReader reader, RestoreMonitor monitor)
        {
            return new WorldRestore(monitor, null)
            {
                WorldWhereami   = reader.GetString("world"),
                MirrorWhereami  = reader.GetString("mirror"),
                //Priority        = reader.GetInt32("priority")
            };
        }

        public async Task<bool> SaveAsync(DbContext db)
        {
            return await db.InsertUpdateAsync(TABLE, new Dictionary<string, object>()
            {
                ["world"]       = WorldWhereami,
                ["mirror"]      = MirrorWhereami,
                ["priority"]    = Priority
            }) >= 0;
        }
    }
}
