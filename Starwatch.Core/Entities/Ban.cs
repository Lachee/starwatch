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
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Starwatch.Database;
using Starwatch.Starbound;
using Starwatch.Util;

namespace Starwatch.Entities
{
    public class Ban : IRecord
    {
        public string Table => "!bans";

        [JsonIgnore]
        public int ServerId { get; set; } = 1;

        [JsonProperty("ticket", NullValueHandling = NullValueHandling.Ignore)]
        public long? Ticket { get; internal set; }

        [JsonProperty("ip", NullValueHandling = NullValueHandling.Ignore)]
        public string IP { get; set; }

        [JsonProperty("uuid", NullValueHandling = NullValueHandling.Ignore)]
        public string UUID { get; set; }

        [JsonProperty("reason", NullValueHandling = NullValueHandling.Ignore)]
        public string Reason { get; set; }

        [JsonProperty("bannedBy", NullValueHandling = NullValueHandling.Ignore)]
        public string Moderator { get; set; }

        [JsonProperty("bannedAt", NullValueHandling = NullValueHandling.Ignore)]
        private long _bannedAt
        {
            get => CreatedDate.HasValue ? CreatedDate.Value.ToUnixEpoch() : 0;
            set => CreatedDate = value.ToDateTime();
        }

        [JsonProperty("expiresAt", NullValueHandling = NullValueHandling.Ignore)]
        private long? _expiresAt
        {
            get => ExpiryDate?.ToUnixEpoch();
            set => ExpiryDate = value?.ToDateTime();
        }

        /// <summary>
        /// The time the ban started
        /// </summary>
        [JsonIgnore]
        public DateTime? CreatedDate { get; internal set; }

        /// <summary>
        /// The time the ban ended
        /// </summary>
        [JsonIgnore]
        public DateTime? ExpiryDate { get; internal set; }

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
        public Ban(Player player, string reason, string moderator) : this()
        {
            if (player == null)
                throw new ArgumentNullException("player");

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
            ExpiryDate = null;
            CreatedDate = DateTime.UtcNow;
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
                CreatedDate = this.CreatedDate ?? other.CreatedDate,
                ExpiryDate = this.ExpiryDate ?? other.ExpiryDate
            };
        }


        /// <summary>
        /// Formats the ban reason
        /// </summary>
        /// <returns></returns>
        public string GetFormattedReason()
        {
            string reason = Reason;

            //Make sure it has the ticket listed at least once.
            if (!reason.Contains("{ticket}")) reason += "\n^orange;Ban Ticket: ^white;{ticket}";

            //Replace the keys
            reason = reason.Replace("{ticket}", Ticket.ToString());
            reason = reason.Replace("{moderator}", Moderator);

            //Replace the expiry date. If it has been set but we have got a date, add it
            if (ExpiryDate.HasValue && (!reason.Contains("{expire}") || !reason.Contains("{expire_time}")))
                reason += "\n^orange;Expires ^white;{expire}";

            //Replace the dates
            reason = reason.Replace("{expire}", ExpiryDate.HasValue ? ExpiryDate.Value.ToString("dd MMM y hh:mm tt") : "never");
            reason = reason.Replace("{expire_time}", ExpiryDate.HasValue ? (ExpiryDate.Value - DateTime.UtcNow).Format() : "never");
            return reason;
        }

        public override string ToString()
        {
            return $"Ban ({Ticket.GetValueOrDefault(-1)}) [{BanType}] {IP ?? ""} {UUID ?? ""}";
        }

        public static async Task<List<Ban>> LoadAllAsync(DbContext db, bool includeExpired)
        {
            //We are manually implementing the select because we have to do a strange thing
            StringBuilder qb = new StringBuilder("SELECT * FROM !bans WHERE server=?server");
            var arguments = new Dictionary<string, object>() { { "server", 1 } };

            if (!includeExpired)
            {
                qb.Append(" AND date_expired > ?now OR date_expired IS NULL");
                arguments.Add("now", DateTime.UtcNow);
            }
            
            return await db.ExecuteAsync<Ban>(qb.ToString(), FromDbDataReader, arguments);
        }

        public async Task<bool> LoadAsync(DbContext db)
        {
            var b = await db.SelectOneAsync<Ban>(Table, FromDbDataReader, new Dictionary<string, object> { { "ticket", Ticket.Value } });
            if (b == null || b.BanType == BanType.Invalid) return false;

            Ticket = b.Ticket;
            UUID = b.UUID;
            IP = b.IP;
            Moderator = b.Moderator;
            Reason = b.Reason;
            CreatedDate = b.CreatedDate;
            ExpiryDate = b.ExpiryDate;
            return true;
        }

        public static Ban FromDbDataReader(DbDataReader reader)
        {
            return new Ban()
            {
                Ticket = reader.GetInt64("ticket"),
                UUID = reader.GetString("uuid"),
                IP = reader.GetString("ip"),
                Moderator = reader.GetString("moderator"),
                Reason = reader.GetString("reason"),
                CreatedDate = reader.GetDateTimeNullable("date_created"),
                ExpiryDate = reader.GetDateTimeNullable("date_expired")
            };
        }

        public async Task<bool> SaveAsync(DbContext db)
        {
            var data = new Dictionary<string, object>()
            {
                { "ip", IP },
                { "uuid", UUID },
                { "reason", Reason },
                { "moderator", Moderator },
                { "server", ServerId }
            };

            if (CreatedDate.HasValue)
                data.Add("date_created", CreatedDate.Value);

            if (ExpiryDate.HasValue)
                data.Add("date_expired", ExpiryDate.Value);

            //We are going to ignore legacy bans from now on!
            //TODO: Create a ticket number
            //if (!Ticket.HasValue) return false;

            //If we have a ticket, add it
            if (Ticket.HasValue && Ticket.Value > 0)
                data.Add("ticket", Ticket.Value);

            //Save the data
            long ticket = await db.InsertUpdateAsync(Table, data);
            if (ticket > 0) Ticket = ticket;
            return ticket > 0;
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
