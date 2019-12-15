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
        public string GameServerBind { get; internal set; }

        [JsonProperty("gameServerPort")]
        public int GameServerPort { get; internal set; }

        [JsonProperty("maxPlayers")]
        public int MaxPlayers { get; set; }

        [JsonProperty("maxTeamSize")]
        public int MaxTeamSize { get; set; }
        #endregion

        #region Query Settings
        [JsonProperty("queryServerBind")]
        public string QueryServerBind { get; internal set; }

        [JsonProperty("queryServerPort")]
        public int QueryServerPort { get; internal set; }

        [JsonProperty("runQueryServer")]
        public bool RunQueryServer { get; set; }
        
        [JsonProperty("serverFidelity")]
        public string ServerFidelity { get; set; }
        
        [JsonProperty("serverOverrideAssetsDigest")]
        public string ServerOverrideAssetsDigest { get; private set; }
        #endregion

        #region Rcon Settings
        [JsonProperty("rconServerBind")]
        public string RconServerBind { get; internal set; }

        [JsonProperty("rconServerPort")]
        public int RconServerPort { get; internal set; }

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
        public AccountList ServerUsers { get; set; }

        #region Bans
        [JsonProperty("bannedIPs")]
        private List<Ban> BannedIps { get; set; }

        [JsonProperty("bannedUuids")]
        private List<Ban> BannedUuids { get; set; }
        #endregion

        public Settings()
        {
            ServerUsers = new AccountList();
            BannedIps = new List<Ban>();
            BannedUuids = new List<Ban>();
        }

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
        /// Adds a range on bans
        /// </summary>
        /// <param name="bans"></param>
        public void AddBanRange(IEnumerable<Ban> bans)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            foreach (var b in bans) AddBan(b);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        /// <summary>
        /// Adds the ban to the banned lists, incrementing the ticket and returning it afterwards.
        /// <para>Ticket value will update if its not already set.</para>
        /// <para>BannedAt value will update if its not already set.</para>
        /// <para>Reason value will update, replacing all instances of {ticket} and {moderator} wwith the ticket id and hte moderators name.</para>
        /// </summary>
        /// <param name="ban">The ban to add</param>
        /// <returns>The ticket that was used</returns>
        [System.Obsolete("External use of this function is now obsolete. Please use the configurator.")]
        public long AddBan(Ban ban)
        {
            if (ban.BanType == BanType.Invalid)
            {
                //Failed to add ban because it contained no IP or UUID
                return -1;
            }

            //Increment the ban ticket
            if (!ban.Ticket.HasValue)
                ban.Ticket = -1;

            //Append the current time
            if (!ban.CreatedDate.HasValue)
                ban.CreatedDate = DateTime.UtcNow;

            //Make sure it has the ticket listed at least once.
            if (!ban.Reason.Contains("{ticket}")) ban.Reason += "\n^orange;Ban Ticket: ^white;{ticket}";

            //Make sure it has the ticket listed at least once.
            ban.Reason = ban.GetFormattedReason();

            //Add to the correct array
            var ipban = ban.GetIpBan();
            var uuidban = ban.GetUuidBan();

            if (ipban != null) BannedIps.Add(ipban);
            if (uuidban != null) BannedUuids.Add(uuidban);
            return ban.Ticket.Value;
        }
                
        /// <summary>
        /// Enumerates over all the bans
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Ban> GetBansEnumerable()
        {
            //Iterate over every IP ban.
            // If they dont have a ticket, return it
            // otherwise match it with its uuid ban
            foreach(var ipban in BannedIps)
            {
                if (!ipban.Ticket.HasValue)
                {
                    yield return ipban;
                }
                else
                {
                    var uuidban = BannedIps.FirstOrDefault(b => b.Ticket.HasValue && b.Ticket.Value == ipban.Ticket.Value);
                    yield return ipban.Combine(uuidban);
                }                
            }

            //Iterate over every uuid ban for ones that dont have tickets
            foreach(var uuidban in BannedUuids)
            {
                if (!uuidban.Ticket.HasValue)
                    yield return uuidban;
            }
        }
    }
}
