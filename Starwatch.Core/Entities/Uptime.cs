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
using Starwatch.Database;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Starwatch.Entities
{
    public class Uptime : IRecord
    {
        public string Table => "!uptime";

        public long Id { get; set; }
        public DateTime Started { get; set; }
        public DateTime? Ended { get; set; }
        public string LastLog { get; set; }
        public string Reason { get; set; }

        public Uptime()
        {
            Id = 0;
            Started = DateTime.UtcNow;
            Ended = null;
            LastLog = "";
            Reason = "";
        }
             
        public async Task<bool> LoadAsync(DbContext db)
        {
            Uptime uptime = await db.SelectOneAsync<Uptime>(Table, (reader) =>
            {
                return new Uptime()
                {
                    Id = reader.GetInt64("id"),
                    Started = reader.GetDateTime("date_started"),
                    Ended = reader.GetDateTimeNullable("date_ended"),
                    LastLog = reader.GetString("last_log"),
                    Reason = reader.GetString("reason")
                };
            }, new Dictionary<string, object>() { { "id", Id } });

            if (uptime == null)
                return false;

            Id = uptime.Id;
            Started = uptime.Started;
            Ended = uptime.Ended;
            LastLog = uptime.LastLog;
            Reason = uptime.Reason;
            return true;
        }

        public async Task<bool> SaveAsync(DbContext db)
        {
            Dictionary<string, object> args = new Dictionary<string, object>()
            {
                { "date_started", Started },
                { "date_ended", Ended },
                { "last_log", LastLog },
                { "reason", Reason },
            };

            if (Id > 0) args.Add("id", Id);

            //Insert the database
            var insertedId = await db.InsertUpdateAsync(Table, args);
            if (insertedId > 0)
            {
                Id = insertedId;
                return true;
            }

            return false;

        }

        /// <summary>
        /// Ends any uptime sessions that still exist.
        /// </summary>
        /// <param name="db"></param>
        /// <returns></returns>
        public static async Task<int> EndAllAsync(DbContext db)
        {
            return await db.ExecuteNonQueryAsync("UPDATE !uptime SET date_ended = CURRENT_TIMESTAMP WHERE date_ended IS NULL");
        }

        /// <summary>
        /// Gets the history of the uptime
        /// </summary>
        /// <param name="context"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static async Task<List<Uptime>> GetHistoryAsync(DbContext context, int count = 10)
        {
            return await context.ExecuteAsync<Uptime>("SELECT * FROM !uptime ORDER BY id DESC LIMIT " + count, (reader) =>
            {
                Uptime uptime = new Uptime();
                uptime.Id       = reader.GetInt64("id");
                uptime.Started  = reader.GetDateTime("date_started");
                uptime.Ended    = reader.GetDateTimeNullable("date_ended");
                uptime.Reason   = reader.GetString("reason");
                uptime.LastLog  = reader.GetString("last_log");
                return uptime;
            });
        }
    }
}
