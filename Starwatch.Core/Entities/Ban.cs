using Newtonsoft.Json;
using Starwatch.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace Starwatch.Entities
{
    public class Ban
    {
        [JsonProperty("ip", NullValueHandling = NullValueHandling.Ignore)]
        public string IP { get; set; }

        [JsonProperty("uuid", NullValueHandling = NullValueHandling.Ignore)]
        public string UUID { get; set; }

        [JsonProperty("ticket", NullValueHandling = NullValueHandling.Ignore)]
        public long? Ticket { get; internal set; }

        [JsonProperty("reason", NullValueHandling = NullValueHandling.Ignore)]
        public string Reason { get; set; }

        [JsonProperty("bannedBy", NullValueHandling = NullValueHandling.Ignore)]
        public string Moderator { get; set; }

        [JsonProperty("bannedAt", NullValueHandling = NullValueHandling.Ignore)]
        public long? BannedAt { get; internal set; }

        /// <summary>
        /// Get the current ban type
        /// </summary>
        [JsonIgnore]
        public BanType BanType => (!string.IsNullOrEmpty(IP) ? BanType.IP : BanType.Invalid) | (!string.IsNullOrEmpty(UUID) ? BanType.UUID : BanType.Invalid);


        /// <summary>
        /// Creates a new ban object based of the supplied player
        /// </summary>
        /// <param name="player">The player to create the ban object from</param>
        /// <param name="reason">The reason for the ban</param>
        /// <param name="moderator">The moderator responsible for the ban</param>
        public Ban(Player player, string reason, string moderator)
        {
            IP = player.IP;
            UUID = string.IsNullOrEmpty(player.UUID) ? null : player.UUID;
            Reason = reason;
            Moderator = moderator;
        }

        /// <summary>
        /// Creates a new empty ban
        /// </summary>
        public Ban()
        {
            IP = null;
            UUID = null;
            Ticket = null;
            Reason = null;
            Moderator = null;
            BannedAt = null;
        }

        /// <summary>
        /// Gets a ban that is appropriate for saving into the ips.
        /// </summary>
        /// <returns></returns>
        public Ban GetIpBan() => string.IsNullOrEmpty(IP) ? null : this;

        /// <summary>
        /// Gets a ban that is appropraite for saving into the uuids
        /// </summary>
        /// <returns></returns>
        public Ban GetUuidBan() => string.IsNullOrEmpty(UUID) ? null : this;

        public Ban Combine(Ban other)
        {
            if (other == null) return this;
            if (this.Ticket != other.Ticket) return null;
            return new Ban()
            {
                Ticket = this.Ticket,
                IP = this.IP ?? other.IP,
                UUID = this.UUID ?? other.UUID,
                Reason = this.Reason ?? other.Reason,
                Moderator = this.Moderator ?? other.Moderator,
                BannedAt = this.BannedAt ?? other.BannedAt
            };
        }

        public override string ToString()
        {
            return $"Ban ({Ticket.GetValueOrDefault(-1)}) [{BanType}] {IP ?? ""} {UUID ?? ""}";
        }
    }

    public enum BanType
    {
        /// <summary>
        /// The ban is invalid
        /// </summary>
        Invalid = 0,

        /// <summary>
        /// Ban contains IP
        /// </summary>
        IP = 1,

        /// <summary>
        /// Ban contains UUID
        /// </summary>
        UUID = 2,

        /// <summary>
        /// Ban is both IP and UUID
        /// </summary>
        Complete = IP | UUID
    }
    
}
