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
using Newtonsoft.Json.Linq;
using Starwatch.Database;
using Starwatch.Entities;
using Starwatch.Logging;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Starwatch.Starbound
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class Configurator
    {
        public Logger Logger { get; }
        public Server Server { get; }
        public DbContext DbContext => Server.DbContext;

        #region Events
        public delegate void AccountUpdateEvent(Account account);
        public delegate void AccountRemoveEvent(string accountName);

        public event AccountUpdateEvent OnAccountAdd;
        public event AccountRemoveEvent OnAccountRemove;
        public event AccountUpdateEvent OnAccountUpdate;
        #endregion

        #region Settings
        /// <summary>
        /// The name of the server
        /// </summary>
        [JsonProperty]
        public string ServerName { get; set; }

        /// <summary>
        /// Allow anonymous connections onto the server
        /// </summary>
        [JsonProperty]
        public bool AllowAnonymousConnections { get; set; }

        /// <summary>
        /// Allow assets mismatch
        /// </summary>
        [JsonProperty]
        public bool AllowAssetsMismatch { get; set; }

        /// <summary>
        /// The max players allowed
        /// </summary>
        [JsonProperty]
        public int MaxPlayers { get; set; }

        /// <summary>
        /// Is the configurator valid
        /// </summary>
        [JsonIgnore]
        public bool IsValid { get; set; }

        #region Bindings
        /// <summary> Address binding for the server </summary>
        [JsonProperty]
        public string GameServerBind { get; set; }

        /// <summary> Port for the server </summary>
        [JsonProperty]
        public int GameServerPort { get; set; }

        /// <summary> Address binding for the query </summary>
        [JsonProperty]
        public string QueryServerBind { get; set; }

        /// <summary> Port for the query </summary>
        [JsonProperty]
        public int QueryServerPort { get; set; }

        /// <summary> Address binding for the rcon </summary>
        public string RconServerBind { get; set; }

        /// <summary> Port for the rcon </summary>
        public int RconServerPort { get; set; }

        /// <summary> Password for the rcon </summary>
        public string RconServerPassword { get; set; }
        #endregion
        #endregion

        public Configurator(Server server, Logger logger = null)
        {
            Server = server;
            Logger = logger ?? new Logger("CNF");
        }

        /// <summary>
        /// Loads the configuration and returns a boolean if it was valid.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> TryLoadAsync()
        {
            await DbContext.SelectAsync<Configurator>("!servers", (reader) => {
                ServerName = reader.GetString("name");
                AllowAnonymousConnections = reader.GetBoolean("allow_anonymous_connections");
                AllowAssetsMismatch = reader.GetBoolean("allow_assets_mismatch");
                MaxPlayers = reader.GetInt32("max_players");

                GameServerBind = reader.GetString("game_bind");
                GameServerPort = reader.GetInt32("game_port");

                QueryServerBind = reader.GetString("query_bind");
                QueryServerPort = reader.GetInt32("query_port");

                RconServerBind = reader.GetString("rcon_bind");
                RconServerPort = reader.GetInt32("rcon_port");
                RconServerPassword = DbContext.Decrypt(reader.GetString("rcon_password"));

                IsValid = !string.IsNullOrWhiteSpace(ServerName) && !string.IsNullOrWhiteSpace(reader.GetString("json"));
                return this;
            }, new Dictionary<string, object>() { { "id", Server.Id } });
            return IsValid;
        }
        
        public async Task SaveAsync(bool reload)
        {
            var data = new Dictionary<string, object>()
            {
                { "id", Server.Id },
                { "name", ServerName },
                { "allow_anonymous_connections", AllowAnonymousConnections },
                { "allow_assets_mismatch", AllowAssetsMismatch },
                { "max_players", MaxPlayers },
                { "game_bind", GameServerBind },
                { "game_port", GameServerPort },
                { "query_bind", QueryServerBind },
                { "query_port", QueryServerPort },
                { "rcon_bind", RconServerBind },
                { "rcon_password", DbContext.Encrypt(RconServerPassword) }
            };

            //Insert the data
            _ = await DbContext.InsertUpdateAsync("!servers", data);

            //Since we are reloading, we will write all the configuration to the file
            if (reload)
                await Server.SaveConfigurationAsync(true);
        }


        /// <summary>
        /// Exports the database entries to a settings object
        /// </summary>
        /// <returns></returns>
        public async Task<Settings> ExportSettingsAsync()
        {
            //Start a timer
            var stopwatch = Stopwatch.StartNew();
            Logger.Log("Exporting Settings");
            
            //Generate the settings
            Settings settings = await DbContext.SelectOneAsync<Settings>("!servers", (reader) =>
            {
                Settings tmp = JsonConvert.DeserializeObject<Settings>(reader.GetString("json"), new Serializer.UserAccountsSerializer());

                tmp.ServerName = reader.GetString("name");
                tmp.AllowAnonymousConnections = reader.GetBoolean("allow_anonymous_connections");
                tmp.AllowAssetsMismatch = reader.GetBoolean("allow_assets_mismatch");
                tmp.MaxPlayers = reader.GetInt32("max_players");

                tmp.GameServerBind = reader.GetString("game_bind");
                tmp.GameServerPort = reader.GetInt32("game_port");

                tmp.QueryServerBind = reader.GetString("query_bind");
                tmp.QueryServerPort = reader.GetInt32("query_port");

                tmp.RconServerBind = reader.GetString("rcon_bind");
                tmp.RconServerPort = reader.GetInt32("rcon_port");
                tmp.RconServerPassword = DbContext.Decrypt(reader.GetString("rcon_password"));

                return tmp;
            }, new Dictionary<string, object>() { { "id", Server.Id } });

            //Add the accounts
            Logger.Log("- Exporting Accounts");
            var accounts = await Account.LoadAllActiveAsync(DbContext);
            settings.ServerUsers.SetAccounts(accounts);
            
            //Add the bans
            Logger.Log("- Exporting Bans");
            var bans = await Ban.LoadAllAsync(DbContext, false);
            settings.AddBanRange(bans);

            Logger.Log("- Done. Took " + stopwatch.ElapsedMilliseconds);

            //Return the settings
            return settings;
        }

        /// <summary>
        /// Imports the configuration file
        /// </summary>
        /// <param name="filepath"></param>
        /// <returns></returns>
        public async Task ImportSettingsAsync(string filepath)
        {
            //Start a timer
            var stopwatch = Stopwatch.StartNew();

            Logger.Log("Importing " + filepath);
            string settingContents = File.ReadAllText(filepath);
            var settings = JsonConvert.DeserializeObject<Settings>(settingContents, new Serializer.UserAccountsSerializer());

            //Save all the bans
            Logger.Log("- Importing Bans @ " + stopwatch.ElapsedMilliseconds);
            foreach (Ban b in settings.GetBansEnumerable())
                await b.SaveAsync(DbContext);

            //Save all the accounts
            Logger.Log("- Importing Accounts @ " + stopwatch.ElapsedMilliseconds);
            foreach (Account a in settings.ServerUsers.Accounts.Values)
                await a.SaveAsync(DbContext);

            //Serialize into a jobj
            Logger.Log("- Importing Settings @ " + stopwatch.ElapsedMilliseconds);
            var jobj = JObject.FromObject(settings);
            jobj.Remove("serverUsers");
            jobj.Remove("bannedIPs");
            jobj.Remove("bannedUuids");
            jobj.Remove("rcon_password");

            //Save the other data
            var data = new Dictionary<string, object>()
            {
                { "id", Server.Id },
                { "name", settings.ServerName },
                { "allow_anonymous_connections", settings.AllowAnonymousConnections },
                { "allow_assets_mismatch", settings.AllowAssetsMismatch },
                { "max_players", settings.MaxPlayers },
                { "game_bind", settings.GameServerBind },
                { "game_port", settings.GameServerPort },
                { "query_bind", settings.QueryServerBind },
                { "query_port", settings.QueryServerPort },
                { "rcon_bind", settings.RconServerBind },
                { "rcon_password", DbContext.Encrypt(settings.RconServerPassword) },
                { "json", jobj.ToString() }
            };

            //Insert the data
            _ = await DbContext.InsertUpdateAsync("!servers", data);
            Logger.Log("- Finished. Took " + stopwatch.ElapsedMilliseconds);
        }

        #region Bans
        /// <summary>
        /// Adds a ban
        /// </summary>
        /// <param name="ban"></param>
        /// <returns></returns>
        public async Task<long> AddBanAsync(Ban ban)
        {
            await ban.SaveAsync(DbContext);
            return ban.Ticket.Value;
        }

        /// <summary>
        /// Gets a ban asyncronously.
        /// </summary>
        /// <param name="ticket"></param>
        /// <returns></returns>
        public async Task<Ban> GetBanAsync(long ticket)
        {
            Ban ban = new Ban() { Ticket = ticket };
            if (await ban.LoadAsync(DbContext))
                return ban;
            return null;
        }

        /// <summary>
        /// Removes a ban
        /// </summary>
        /// <param name="ticket"></param>
        /// <returns></returns>
        public async Task<bool> ExpireBanAsync(Ban ban)
        {
            if (ban == null) return false;
            ban.ExpiryDate = DateTime.UtcNow;
            await ban.SaveAsync(DbContext);
            return true;
        }

        /// <summary>
        /// Removes a ban
        /// </summary>
        /// <param name="ticket"></param>
        /// <returns></returns>
        public async Task<bool> ExpireBanAsync(long ticket)
        {
            var ban = await GetBanAsync(ticket);
            return await ExpireBanAsync(ban);
        }
        #endregion

        #region Accounts

        /// <summary>
        /// Gets an account
        /// </summary>
        /// <param name="accountName"></param>
        /// <returns></returns>
        public async Task<Account> GetAccountAsync(string accountName, DbContext context = null)
        {
            context = context ?? this.DbContext;

            Account account = new Account(accountName);
            if (await account.LoadAsync(context)) return account;
            return null;
        }

        /// <summary>
        /// Removes an account
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public async Task<bool> RemoveAccountAsync(Account account, DbContext context = null)
        {
            context = context ?? this.DbContext;
            return await context.DeleteAsync(account.Table, new Dictionary<string, object>(){ { "name", account.Name } });
        }

        /// <summary>
        /// Sets an account. Returns true if the account was updated.
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public async Task<bool> SetAccountAsync(Account account, DbContext context = null)
        {
            context = context ?? this.DbContext;

            if (await account.SaveAsync(context))
            {
                OnAccountAdd?.Invoke(account);
                return true;
            }
            else
            {
                OnAccountUpdate?.Invoke(account);
                return false;
            }
        }
        #endregion
    }
}
