using Starwatch.Extensions.Backup;
using System;

namespace Starwatch.API.Rest.Route.Entities
{
    public struct OptionalWorldBackup
    {
        /// <summary>
        /// Is this a rolling backup?
        /// </summary>
        public bool? IsRolling { get; internal set; }

        /// <summary>
        /// Is this a auto restore?
        /// </summary>
        public bool? IsAutoRestore { get; internal set; }

        /// <summary>
        /// The time the last backup took place.
        /// </summary>
        public DateTime? LastBackup { get; private set; }

        public OptionalWorldBackup(WorldBackup worldBackup)
        {
            IsRolling = worldBackup.IsRolling;
            IsAutoRestore = worldBackup.IsAutoRestore;
            LastBackup = worldBackup.LastBackup;
        }
    }
}
