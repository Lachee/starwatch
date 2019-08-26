using Newtonsoft.Json;
using Starwatch.Extensions.Backup;
using System;

namespace Starwatch.API.Rest.Route.Entities
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class OptionalWorldBackup
    {
        /// <summary>
        /// Is this a rolling backup?
        /// </summary>
        public bool? IsRolling { get; set; }

        /// <summary>
        /// Is this a auto restore?
        /// </summary>
        public bool? IsAutoRestore { get; set; }

        /// <summary>
        /// The time the last backup took place.
        /// </summary>
        public DateTime? LastBackup { get; set; }

        public OptionalWorldBackup() { }

        public OptionalWorldBackup(WorldBackup worldBackup)
        {
            IsRolling = worldBackup.IsRolling;
            IsAutoRestore = worldBackup.IsAutoRestore;
            LastBackup = worldBackup.LastBackup;
        }
    }
}
