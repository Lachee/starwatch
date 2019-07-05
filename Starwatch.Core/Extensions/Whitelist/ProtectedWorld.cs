﻿using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Starwatch.Database;
using Starwatch.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Starwatch.Extensions.Whitelist
{
    public class ProtectedWorld : IRecord
    {
        public const string TableName = "!protections";
        public string Table => TableName;

        /// <summary>
        /// Unique ID for the protection
        /// </summary>
        public long Id { get; internal set; }

        [JsonIgnore]
        public WhitelistManager Manager { get; }

        /// <summary>
        /// Name of hte protection
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The world being protected
        /// </summary>
        [JsonIgnore]
        public World World { get; private set; }

        /// <summary>
        /// Whereami Location of the world
        /// </summary>
        public string Whereami { get => World?.Whereami; set => World = World.Parse(value); }

        /// <summary>
        /// Allow anonymous connections?
        /// </summary>
        public bool AllowAnonymous { get; set; }

        /// <summary>
        /// The mode of the world.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public WhitelistMode Mode { get; set; }

        private ProtectedWorld()
        {
            this.Manager = null;
            this.World = null;
            this.Id = 0;
        }

        public ProtectedWorld(WhitelistManager manager, World world)
        {
            this.Manager = manager;
            this.World = world;
            this.Id = 0;
        }
        
        /// <summary>
        /// Checks if the account is allowed in this world
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public async Task<bool> CheckPermissionAsync(Account account)
        {
            if (account == null) return AllowAnonymous;

            bool listed = (await GetAccountAsync(account)) != null;
            return Mode == WhitelistMode.Whitelist ? listed : !listed;
        }

        /// <summary>
        /// Checks iif hte player is allowed in this world
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public async Task<bool> CheckPermissionAsync(Player player)
        {
            if (player.IsAnonymous) return AllowAnonymous;

            var account = await player.GetAccountAsync();
            return await CheckPermissionAsync(account);
        }

        /// <summary>
        /// Gets the account listing
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public async Task<ListedAccount> GetAccountAsync(Account account)
        {
            var listedAccount = new ListedAccount(this, account.Name);
            if (await listedAccount.LoadAsync(Manager.DbContext))
                return listedAccount;
            return null;
        }

        /// <summary>
        /// Adds an account to the listing
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public async Task<ListedAccount> SetAccountAsync(Account account, string reason)
        {
            var listedAccount = new ListedAccount(this, account.Name) { Reason = reason };
            await listedAccount.SaveAsync(Manager.DbContext);
            return listedAccount;
        }

        /// <summary>
        /// Removes an account from the listing
        /// </summary>
        /// <param name="account"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        public async Task<bool> RemoveAccountAsync(Account account)
        {
            var acc = await GetAccountAsync(account);
            if (acc == null) return false;
            return await acc.DeleteAsync(Manager.DbContext);
        }

        /// <summary>
        /// Deletes the protection
        /// </summary>
        /// <returns></returns>
        public async Task<bool> DeleteAsync()
        {
            await ListedAccount.DeleteAllAsync(Manager.DbContext, this.Id);
            return await Manager.DbContext.DeleteAsync(Table, new Dictionary<string, object>() { { "id", this.Id } });
        }

        public async Task<bool> LoadAsync(DbContext db)
        {
            var args = new Dictionary<string, object>();
            if (Id > 0) args.Add("id", Id);
            else        args.Add("whereami", Whereami);

            var pw = await db.SelectOneAsync<ProtectedWorld>(Table, (reader) =>
            {
                return new ProtectedWorld()
                {
                    Id = reader.GetInt64("id"),
                    Whereami = reader.GetString("whereami"),
                    Mode = reader.GetString("mode") == "BLACKLIST" ? WhitelistMode.Blacklist : WhitelistMode.Whitelist,
                    AllowAnonymous = reader.GetBoolean("allow_anonymous"),
                    Name = reader.GetString("name")
                };
            }, args);

            if (pw == null) return false;

            Id = pw.Id;
            Whereami = pw.Whereami;
            Mode = pw.Mode;
            AllowAnonymous = pw.AllowAnonymous;
            Name = pw.Name;
            return true;
        }

        public async Task<bool> SaveAsync(DbContext db)
        {
            var args = new Dictionary<string, object>()
            {
                { "whereami", Whereami },
                { "mode", Mode == WhitelistMode.Blacklist ? "BLACKLIST" : "WHITELIST" },
                { "allow_anonymous", AllowAnonymous }
            };

            if (Id > 0)
                args.Add("id", Id);

            var insertedId = await db.InsertUpdateAsync(Table, args);
            if (insertedId > 0)
            {
                Id = insertedId;
                return true;
            }

            return false;
        }
    }
}
