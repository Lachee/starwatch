using Newtonsoft.Json;
using Starwatch.Database;
using Starwatch.Entities;
using Starwatch.Monitoring;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starwatch.Starbound
{
    public class Session : IRecord
    {
        [JsonIgnore]
        public string Table => "!sessions";

        public long Id { get; private set; }

        public long UptimeId { get; private set; }

        [JsonIgnore]
        public Server Server { get; }
        
        [JsonIgnore]
        public Player Player { get; set; }

        public int Connection => Player.Connection;
        public string IP => Player.IP;
        public string UUID => Player.UUID;
        public string Username => Player.Username;
        public string Account => Player.AccountName;
        
        public DateTime ConnectedAt { get; set; }
        public DateTime? DisconnectedAt { get; set; }

        public Session(Server server, long id = 0)
        {
            Server = server;
            ConnectedAt = DateTime.Now;
            Id = id;

            var monitor = server.GetMonitors<UptimeMonitor>().FirstOrDefault();
            if (monitor != null) UptimeId = monitor.CurrentUptimeId;
        }

        public Session(Player player) : this(player.Server)
        {
            Player = player;
        }

        public async Task<bool> LoadAsync(DbContext db)
        {
            var s = await db.SelectOneAsync<Session>(Table, (reader) => FromDbDataReader(reader, Server), new Dictionary<string, object>() { { "id", Id } });
            if (s == null) return false;

            Player = s.Player;
            UptimeId = s.UptimeId;
            ConnectedAt = s.ConnectedAt;
            DisconnectedAt = s.DisconnectedAt;
            return true;
        }

        private static Session FromDbDataReader(DbDataReader reader, Server server)
        {
            //Prepare the player
            int cid = reader.GetInt32("cid");
            var player = new Player(server, cid)
            {
                IP = reader.GetString("ip"),
                UUID = reader.GetString("uuid"),
                Username = reader.GetString("username"),
                AccountName = reader.GetString("account"),
            };

            //Build the session
            return new Session(player)
            {
                UptimeId = reader.GetInt64("uptime"),
                ConnectedAt = reader.GetDateTime("date_joined"),
                DisconnectedAt = reader.GetDateTimeNullable("date_left"),
                Id = reader.GetInt64("id")
            };
        }

        public async Task<bool> SaveAsync(DbContext db)
        {
            var data = new Dictionary<string, object>()
            {
                { "cid", Player.Connection },
                { "ip", Player.IP },
                { "uuid", Player.UUID },
                { "username", Player.Username },
                { "username_clean", new TaggedText(Player.Username).Content },
                { "account", Player.AccountName },
                { "date_joined", ConnectedAt },
                { "uptime", UptimeId }
            };

            if (DisconnectedAt.HasValue)
                data.Add("date_left", DisconnectedAt);

            if (Id > 0)
                data.Add("id", Id);

            var inserted = await db.InsertUpdateAsync(Table, data);
            if (inserted > 0)
            {
                Id = inserted;
                return true;
            }

            return false;
        }

        public async Task FinishAsync()
        {
            DisconnectedAt = DateTime.UtcNow;
            await SaveAsync(Server.DbContext);
        }

        /// <summary>
        /// Finishes all previous sessions
        /// </summary>
        /// <param name="db"></param>
        /// <returns></returns>
        public static async Task<int> FinishAllAsync(DbContext db)
        {
            return await db.ExecuteNonQueryAsync("UPDATE !sessions SET date_left = CURRENT_TIMESTAMP WHERE date_left IS NULL");
        }

        public static async Task<List<Session>> FindAsync(DbContext db, Server server, Dictionary<string, object> parameters, string mode = "OR", TimeSpan? duration = null, int limit = 20)
        {
            //Invalid seach parameters
            if (!duration.HasValue && parameters.Count == 0)
                return new List<Session>();

            //Clone the argument list
            var arguments = new Dictionary<string, object>(parameters);

            //Prepare the condition string and the query
            string condition = string.Join($" {mode} ", arguments.Keys.Select(k => k + " LIKE ?" + k ));
            string query = $"SELECT * FROM !sessions WHERE {condition}";
                        
            //Add the date filter
            if (duration.HasValue)
            {
                arguments.Add("date_joined", DateTime.UtcNow - duration);
                query += (arguments.Count > 1 ? " AND" : "" ) + " date_joined > ?date_joined";
            }

            //Add the limit
            query += " ORDER BY id DESC LIMIT " + limit;

            //Execute
            return await db.ExecuteAsync<Session>(query, (reader) => FromDbDataReader(reader, server), arguments);
        }
    }
}
