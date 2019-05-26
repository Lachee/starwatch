using Starwatch.Entities;
using Starwatch.Monitoring;
using Starwatch.Starbound;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;
using System.Linq;

namespace Starwatch.Extensions.Backup
{
    public class BackupManager : ConfigurableMonitor
    {
        private const string CONFIG_WORLDS_KEY = "worlds";
        private const string CONFIG_BACKUP_DIRECTORY_KEY = "backup_directory";
        private const string CONFIG_INTERVAL_KEY = "inverval";

        private List<WorldBackup> _backups = new List<WorldBackup>();
        private object _backupsLock = new object();

        public string UniverseDirectory => $@"{Server.StorageDirectory}\universe";
        public string BackupDirectory => Configuration.GetString(CONFIG_BACKUP_DIRECTORY_KEY, $"{UniverseDirectory}_backups");

        private Timer _rollbackTimer;

        public BackupManager(Server server) : base(server, "Backup")
        {
        }

        public override Task Initialize()
        {
            //Load all the backups
            _backups = Configuration.GetObject<List<WorldBackup>>(CONFIG_WORLDS_KEY, new List<WorldBackup>());
            foreach (var b in _backups) b.Initialize(this);

            //Initialize the timer
            _rollbackTimer = new Timer(Configuration.GetInt(CONFIG_INTERVAL_KEY, 10) * 60000) { AutoReset = true };
            _rollbackTimer.Elapsed += (sender, evt) =>
            {
                int delta = Configuration.GetInt(CONFIG_INTERVAL_KEY, 10);
                lock (_backupsLock)
                {
                    foreach (var b in _backups)
                    {
                        if (b.IsRolling) b.RollBackup();
                    }
                }
            };
            _rollbackTimer.Start();
            
            //Send the completed
            return Task.CompletedTask;
        }
        public override Task OnServerPreStart()
        {
            //Copy over any worlds that are set to autorestore
            foreach(var b in _backups)
                if (b.IsAutoRestore) b.RestoreFile();
            
            //Return the base event
            return base.OnServerPreStart();
        }
        
        /// <summary>
        /// Backups the world and returns true if succesful.
        /// </summary>
        /// <param name="world">The world to backup</param>
        /// <returns></returns>
        public bool Backup(World world)
        {
            if (world.Filename == null) return false;

            var backup = _backups.FirstOrDefault(wb => wb.World.Equals(world));
            if (backup == default(WorldBackup)) return false;
            backup.Backup();
            return true;
        }

        /// <summary>
        /// Adds a world to the backups
        /// </summary>
        /// <param name="world">The world to add</param>
        /// <param name="rollingBackups">Should the world follow rolling backups?</param>
        /// <param name="autoRestore">Should the world auto restore?</param>
        /// <returns></returns>
        public bool Add(World world, bool rollingBackups, bool autoRestore)
        {
            if (world.Filename == null) return false;

            lock (_backupsLock)
            {
                //Make sure the world doesnt already exist
                if (_backups.Any(wb => wb.World.Equals(world)))
                    return false;

                //Initialize the new world
                WorldBackup backup = new WorldBackup(world)
                {
                    IsRolling = rollingBackups,
                    IsAutoRestore = autoRestore
                };
                backup.Initialize(this);

                //Create the initial backup
                backup.Backup();

                //Add the world
                _backups.Add(backup);
                Configuration.SetKey(CONFIG_WORLDS_KEY, _backups, save: true);
                return true;
            }
        }

        /// <summary>
        /// Removes a world from the backups
        /// </summary>
        /// <param name="world">The name of the world</param>
        /// <param name="deleteFiles">Should the files be deleted?</param>
        /// <returns></returns>
        public bool Remove(World world, bool deleteFiles = false)
        {
            lock (_backupsLock)
            {
                if (deleteFiles)
                {
                    //Go through deleting all the backup files.
                    foreach(var backup in _backups.Where(wb => wb.World.Equals(world)))
                        backup.DeleteAllFiles();
                }

                //Make sure the world doesnt already exist
                bool success = _backups.RemoveAll(wb => wb.World.Equals(world)) > 0;
                Configuration.SetKey(CONFIG_WORLDS_KEY, _backups, save: success);
                return success;
            }
        }

        /// <summary>
        /// Gets the backup for a world
        /// </summary>
        /// <param name="world"></param>
        /// <returns></returns>
        public WorldBackup GetBackup(World world)
        {
            lock(_backupsLock)
            {
                return _backups.Where(wb => wb.World.Equals(world)).FirstOrDefault(null);
            }
        }
        
        public override void Dispose()
        {
            if (_rollbackTimer != null)
            {
                _rollbackTimer.Dispose();
                _rollbackTimer = null;
            }
        }
    }
}
