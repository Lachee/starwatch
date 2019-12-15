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
using Starwatch.Starbound;
using System.Threading.Tasks;

namespace Starwatch.Entities
{
    public class Player
    {
        [JsonIgnore]
        public Server Server { get; private set; }

        public int Connection { get; private set; }
        public string Username { get; internal set; }
        public string Nickname { get; internal set; }
        public string AccountName { get; internal set; }
        public string UUID { get; internal set; }
        public string IP { get; internal set; }
        public bool IsAnonymous => string.IsNullOrWhiteSpace(AccountName) || AccountName.Equals(Account.Annonymous);
        public bool IsAdmin { get; internal set; }
        public bool IsVPN { get; internal set; }
        //public bool IsAdmin { get { return (Server?.Settings?.Accounts.GetAccount(AccountName)?.IsAdmin).GetValueOrDefault(false); } }

        [JsonIgnore]
        public World Location { get; private set; }

        [JsonProperty("Location")]
        public string Whereami { get => Location?.Whereami; set { Location = World.Parse(value); } }

        public Player(Player player)
        {
            this.Server = player.Server;
            this.Connection = player.Connection;
            this.Username = player.Username;
            this.Nickname = player.Nickname;
            this.AccountName = player.AccountName;
            this.UUID = player.UUID;
            this.IP = player.IP;
            this.IsAdmin = player.IsAdmin;
            this.Location = player.Location;
            this.IsVPN = player.IsVPN;
        }

        public Player(Server server, int cid)
        {
            this.Server = server;
            this.Connection = cid;
            this.IsVPN = false;
        }

        /// <summary>
        /// Bans the player from the server, automatically reloading the server and kicking the palyer.
        /// </summary>  
        /// <param name="reason">The reason of the ban. This is formatted automatically. {ticket} will be replaced with the Ticket and {moderator} will be replaced with the moderator who added the ban.</param>
        /// <param name="moderator">The author of the ban</param>
        /// <returns></returns>
        public async Task<long> Ban(string reason, string moderator) => await Server.Ban(this, reason, moderator, true, true);
        

        /// <summary>
        /// Kicks the user from the server. Only available if the server has RCON enabled.
        /// </summary>
        /// <param name="reason"></param>
        /// <returns></returns>
        public async Task<Starbound.Rcon.RconResponse> Kick(string reason) => await Server.Kick(this, reason);

        /// <summary>
        /// Gets the account linked to this player.
        /// </summary>
        /// <returns></returns>
        public async Task<Account> GetAccountAsync()
        {
            if (string.IsNullOrEmpty(AccountName)) return null;
            return await Server.Configurator.GetAccountAsync(AccountName);
        }

        /// <summary>
        /// Checks if the player is still connected
        /// </summary>
        /// <returns></returns>
        public bool IsConnected() => Server.Connections.IsConnected(this);

        public override string ToString()
        {
            return $"${Connection} {Username} ({IP})";
        }

    }
}
