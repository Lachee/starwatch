using Newtonsoft.Json;
using Starwatch.Entities;
using Starwatch.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Starwatch.Starbound
{
    public class Settings
    {
        [JsonProperty("serverName")]
        public string ServerName { get; set; }

        [JsonProperty("allowAdminCommands")]
        public bool AllowAdminCommands { get; set; }

        [JsonProperty("allowAdminCommandsFromAnyone")]
        public bool AllowAdminCommandsFromAnyone { get; set; }

        [JsonProperty("allowAnonymousConnections")]
        public bool AllowAnonymousConnections { get; set; }

        [JsonProperty("allowAssetsMismatch")]
        public bool AllowAssetsMismatch { get; set; }

        [JsonProperty("anonymousConnectionsAreAdmin")]
        public bool AnonymousConnectionsAreAdmin { get; set; }

        [JsonProperty("checkAssetsDigest")]
        public bool CheckAssetsDigest { get; set; }

        [JsonProperty("clearPlayerFiles")]
        public bool ClearPlayerFiles { get; set; }

        [JsonProperty("clearUniverseFiles")]
        public bool ClearUniverseFiles { get; set; }

        [JsonProperty("clientIPJoinable")]
        public bool ClientIPJoinable { get; set; }

        [JsonProperty("clientP2PJoinable")]
        public bool ClientP2PJoinable { get; set; }

        [JsonProperty("configurationVersion")]
        public Version ConfigurationVersion { get; private set; }
        public struct Version
        {
            [JsonProperty("basic")]
            public int Basic { get; private set; }

            [JsonProperty("server")]
            public int Server { get; private set; }
        }

        #region Game Server Settings
        [JsonProperty("gameServerBind")]
        public string GameServerBind { get; private set; }

        [JsonProperty("gameServerPort")]
        public int GameServerPort { get; private set; }

        [JsonProperty("maxPlayers")]
        public int MaxPlayers { get; set; }

        [JsonProperty("maxTeamSize")]
        public int MaxTeamSize { get; set; }
        #endregion

        #region Query Settings
        [JsonProperty("queryServerBind")]
        public string QueryServerBind { get; private set; }

        [JsonProperty("queryServerPort")]
        public int QueryServerPort { get; private set; }

        [JsonProperty("runQueryServer")]
        public bool RunQueryServer { get; set; }
        
        [JsonProperty("serverFidelity")]
        public string ServerFidelity { get; set; }
        
        [JsonProperty("serverOverrideAssetsDigest")]
        public string ServerOverrideAssetsDigest { get; private set; }
        #endregion

        #region Rcon Settings
        [JsonProperty("rconServerBind")]
        public string RconServerBind { get; private set; }

        [JsonProperty("rconServerPort")]
        public int RconServerPort { get; private set; }

        [JsonProperty("rconServerPassword")]
        public string RconServerPassword { get; set; }

        [JsonProperty("rconServerTimeout")]
        public int RconServerTimeout { get; set; }

        [JsonProperty("runRconServer")]
        public bool RunRconServer { get; set; }
        #endregion

        #region Script Settings
        [JsonProperty("safeScripts")]
        public bool SafeScripts { get; set; }

        [JsonProperty("scriptInstructionLimit")]
        public long ScriptInstructionLimit { get; set; }

        [JsonProperty("scriptInstructionMeasureInterval")]
        public long ScriptInstructionMeasureInterval { get; set; }

        [JsonProperty("scriptProfilingEnabled")]
        public bool ScriptProfilingEnabled { get; set; }

        [JsonProperty("scriptRecursionLimit")]
        public int ScriptRecursionLimit { get; set; }
        #endregion
        
        [JsonProperty("serverUsers")]
        public AccountList Accounts { get; set; }

        #region Bans
        [JsonProperty("bannedIPs")]
        private List<Ban> BannedIps { get; set; }

        [JsonProperty("bannedUuids")]
        private List<Ban> BannedUuids { get; set; }

        [JsonProperty("bannedTicket")]
        public long CurrentBanTicket { get; private set; }
        #endregion

        /// <summary>
        /// Removes the ban from the ban list.
        /// </summary>
        /// <param name="ban">The ban to remove</param>
        /// <returns>True if successful</returns>
        public bool RemoveBan(Ban ban)
        {
            //Make sure its a valid ban to begin with
            if (ban.BanType == BanType.Invalid) return false;

            //Store its success state then try to remove accounts from each state
            bool success = false;

            if (BannedIps.RemoveAll(b => b.Ticket == ban.Ticket) > 0) success = true;
            if (BannedUuids.RemoveAll(b => b.Ticket == ban.Ticket) > 0) success = true;

            return success;
        }

        /// <summary>
        /// Adds the ban to the banned lists, incrementing the ticket and returning it afterwards.
        /// <para>Ticket value will update if its not already set.</para>
        /// <para>BannedAt value will update if its not already set.</para>
        /// <para>Reason value will update, replacing all instances of {ticket} and {moderator} wwith the ticket id and hte moderators name.</para>
        /// </summary>
        /// <param name="ban">The ban to add</param>
        /// <returns>The ticket that was used</returns>
        public long AddBan(Ban ban)
        {
            if (ban.BanType == BanType.Invalid)
            {
                //Failed to add ban because it contained no IP or UUID
                return -1;
            }

            //Increment the ban ticket
            if (!ban.Ticket.HasValue)
                ban.Ticket = GetNextTicket();

            //Append the current time
            if (!ban.BannedAt.HasValue)
                ban.BannedAt = DateTime.UtcNow.ToUnixEpoch();

            //Make sure it has the ticket listed at least once.
            if (!ban.Reason.Contains("{ticket}")) ban.Reason += "\n^orange;Ban Ticket: ^white;{ticket}";

            //Replace the keys
            ban.Reason = ban.Reason.Replace("{ticket}", ban.Ticket.ToString());
            ban.Reason = ban.Reason.Replace("{moderator}", ban.Moderator);

            //Add to the correct array
            var ipban = ban.GetIpBan();
            var uuidban = ban.GetUuidBan();

            if (ipban != null) BannedIps.Add(ipban);
            if (uuidban != null) BannedUuids.Add(uuidban);
            return ban.Ticket.Value;
        }

        /// <summary>
        /// Gets a ban with the given ticket, combining both IP and UUID ban.
        /// </summary>
        /// <param name="ticket"></param>
        /// <returns></returns>
        public Ban GetBan(long ticket)
        {
            var ipban = BannedIps.FirstOrDefault(b => b.Ticket.HasValue && b.Ticket.Value == ticket);
            var uuidban = BannedIps.FirstOrDefault(b => b.Ticket.HasValue && b.Ticket.Value == ticket);

            if (ipban == null) return uuidban;
            return ipban.Combine(uuidban);
        }
        
        /// <summary>
        /// Increments the ban ticket and return the new one.
        /// </summary>
        /// <returns></returns>
        public long GetNextTicket() => (CurrentBanTicket += 2);
    }
}
