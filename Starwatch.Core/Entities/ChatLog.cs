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
using Starwatch.Monitoring;
using Starwatch.Starbound;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Starwatch.Entities
{
    public class ChatLog : IRecord
    {
        public string Table => "!chat";


        public Server Server { get; }

        public long Id { get; set; }

        public TaggedText Content { get; private set; }
        public string TaggedContent => Content?.TaggedContent;

        public Session Session { get; private set; }
        public string Location { get; private set; }

        public DateTime Created { get; private set; }

        private ChatLog()
        {
            Id = 0;
            Content = null;
            Session = null;
            Created = DateTime.UtcNow;
        }

        public ChatLog(Server server) : this()
        {
            Server = server;
            //if (server.TryGetMonitor<UptimeMonitor>(out var m))
            //    UptimeId = m.CurrentUptimeId;
        }

        public ChatLog(Message message) : this(message.Server)
        {
            if (!message.IsChat)
                throw new ArgumentException("The message has to be a chat message.", "message");

            Content = new TaggedText(message.Content);
            Session = message.GetSession();
            Location = Session.Player.Location.Whereami;
        }

        public async Task<bool> LoadAsync(DbContext db)
        {
            var log = await db.SelectOneAsync<ChatLog>(Table, (reader) =>
            {
                return new ChatLog()
                {
                    Id = reader.GetInt64("id"),
                    Content = reader.GetString("content"),
                    Session = new Session(Server, reader.GetInt64("session")),
                    Location = reader.GetString("location"),
                    Created = reader.GetDateTime("date_created")
                };
            }, new Dictionary<string, object>() { { "id", Id } });

            if (log == null)
                return false;

            Id = log.Id;
            Content = log.Content;
            Session = log.Session;
            Location = log.Location;
            Created = log.Created;
            Session = log.Session;

            if (Session != null)
                await Session.LoadAsync(db);

            return true;
        }

        public async Task<bool> SaveAsync(DbContext db)
        {
            Dictionary<string, object> data = new Dictionary<string, object>()
            {
                { "session", Session.Id },
                { "content", TaggedContent },
                { "content_clean", Content.ToString() },
                { "location", Location }
            };

            if (Id > 0) data.Add("id", Id);

            var inserted = await db.InsertUpdateAsync(Table, data);
            if (inserted > 0)
            {
                Id = inserted;
                return true;
            }

            return false;
        }
    }
}
