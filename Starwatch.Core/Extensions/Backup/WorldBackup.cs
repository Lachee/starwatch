using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Starwatch.Entities;
using Starwatch.Logging;

namespace Starwatch.Extensions.Backup
{
    public class WorldBackup
    {
        [JsonIgnore]
        public BackupManager BackupManager { get; private set; }

        /// <summary>
        /// The world we have been put in charge off
        /// </summary>
        [JsonIgnore] public World World { get; internal set; }
        [JsonProperty("World")] private string WorldSerialize { get => World.Whereami; set { World = World.Parse(value); } }

        /// <summary>
        /// Is this a rolling backup?
        /// </summary>
        public bool IsRolling { get; internal set; }

        /// <summary>
        /// Is this a auto restore?
        /// </summary>
        public bool IsAutoRestore { get; internal set; }

        /// <summary>
        /// The time the last backup took place.
        /// </summary>
        public DateTime LastBackup { get; private set; }

        /// <summary>
        /// Storage directory of the universe
        /// </summary>
        [JsonIgnore]
        public string StorageDirectory => BackupManager.UniverseDirectory;

        /// <summary>
        /// Backup directory of the universe
        /// </summary>
        [JsonIgnore]
        public string BackupDirectory => BackupManager.BackupDirectory;

        [JsonIgnore]
        private Logger Logger { get; set; }

        private int[] _backupIntervals = new int[] { 10, 30, 60, 300, 1200 };
        private List<RollingBackup> _rollingBackups = null;
        private DateTime _lastRollingBackup;

        internal WorldBackup() { }
        public WorldBackup(World world)
        {
            this.World = world;
        }

        public void Initialize(BackupManager manager)
        {
            BackupManager = manager;
            Logger = new Logger(World.Whereami, manager.Logger);

            if (IsRolling)
            {
                _lastRollingBackup = DateTime.Now;
                _rollingBackups = new List<RollingBackup>();
                foreach (var interval in _backupIntervals) _rollingBackups.Add(new RollingBackup(this, interval));
            }

        }

        public void RollBackup()
        {
            //WE dont want to roll a new backup if its unavailable.
            if (IsRolling) return;

            //Calculate the time since updates
            TimeSpan delta = DateTime.Now - _lastRollingBackup;
            int deltaMinutes = (int) Math.Floor(delta.TotalMinutes);

            //Go down the stack!
            for (int i = _rollingBackups.Count - 1; i >= 0; i++)
            {
                //Decrement their counter. If it hits zero we should backup the world
                if (_rollingBackups[i].DecrementCountdown(deltaMinutes))
                {
                    string destFilename = _rollingBackups[i].GetFilename();
                    string sourceFilename = i > 0 ? _rollingBackups[i - 1].GetFilename() : World.Filename;
                    BackupFile(destFilename, sourceFilename);
                }
            }

            //Update the time we rolled last again
            _lastRollingBackup = DateTime.Now;
        }

        /// <summary>
        /// Forces a backup of the world. If the world is rolling, then it will force a backup of the lowest interval.
        /// </summary>
        public void Backup()
        {
            if (!IsRolling)
            {
                //Backup the world in general
                BackupFile(World.Filename, GetBackupFilename());
            }
            else
            {
                //Backup the interval world
                BackupFile(World.Filename, GetIntervalFilename(0));
            }
        }

        /// <summary>
        /// Restores the file with a optional interval
        /// </summary>
        /// <param name="interval">The interval of time to restore from. If 0 or less, then it will be the last backup made.</param>
        public void RestoreFile(int interval = 0)
        {
            string sourceFilename = interval <= 0 ? GetBackupFilename() : GetIntervalFilename(interval);
            string destFilename = World.Filename;

            string source = Path.Combine(BackupDirectory, sourceFilename);
            string dest = Path.Combine(StorageDirectory, destFilename);

            Logger.Log("Restoring file: {0}", sourceFilename);
            if (!File.Exists(source))
            {
                Logger.LogError("Cannot restore because the backup '{0}' does not exist!", source);
                return;
            }

            if (!Directory.Exists(StorageDirectory))
            {
                Logger.LogError("Cannot restore because the destination directory does not exist!", StorageDirectory);
                return;
            }

            //Copy the file
            File.Copy(source, dest, true);
        }
        
        private void BackupFile(string sourceFilename, string destFilename)
        {
            if (sourceFilename == null) sourceFilename = World.Filename;
            string source = Path.Combine(StorageDirectory, sourceFilename);
            string dest = Path.Combine(BackupDirectory, destFilename);

            Logger.Log("Backup of file: {0}", destFilename);
            if (!Directory.Exists(StorageDirectory)) Directory.CreateDirectory(StorageDirectory);
            if (!File.Exists(source))
            {
                Logger.LogError("Cannot backup because the world '{0}' does not exist!", source);
                return;
            }
            
            //Copy the file
            File.Copy(source, dest, true);
            LastBackup = DateTime.Now;
        }
        

        /// <summary>
        /// Gets the filename for the latest backup. If the backup is rolling then it will be the name of the first interval backup.
        /// </summary>
        /// <returns></returns>
        public string GetBackupFilename() =>  IsRolling ? _rollingBackups[0].GetFilename() : $"{World.Filename}.bak";

        /// <summary>
        /// Gets the filename of a rolling backup that matches the target interval.
        /// </summary>
        /// <param name="interval"></param>
        /// <returns></returns>
        public string GetIntervalFilename(int interval)
        {
            //We are 0, so just return the simplest filename.
            if (interval <= 0) return _rollingBackups[0].GetFilename();

            //Scan backwards looking for the first backup that is bigger than the interval.
            for (int i = _backupIntervals.Length; i >= 0; i--)
            {
                if (_rollingBackups[i].Interval >= interval)
                    return _rollingBackups[i].GetFilename();
            }

            //We found nothing so just return the first one.
            return _rollingBackups[0].GetFilename();
        }

        /// <summary>
        /// Deletes all backups.
        /// </summary>
        public void DeleteAllFiles()
        {
            if (IsRolling)
            {
                foreach (var rolling in _rollingBackups)
                    DeleteFile(rolling.GetFilename());
            }
            else
            {
                DeleteFile(GetBackupFilename());
            }
        }

        private void DeleteFile(string filename)
        {
            string src = Path.Combine(BackupDirectory, filename);
            if (File.Exists(src)) File.Delete(src);
        }
    }
}
