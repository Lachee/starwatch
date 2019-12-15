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
using System;
using System.Collections.Generic;
using System.Text;

namespace Starwatch.Database
{
    public struct ConnectionSettings
    {
        public string Host { get; set; }
        public string Database { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Prefix { get; set; }
        public string Passphrase { get; set; }

        public string DefaultImport { get; set; }

        public string ConnectionStringOverride { get; set; }

        [JsonIgnore]
        public string ConnectionString => !string.IsNullOrWhiteSpace(ConnectionStringOverride) ? 
            ConnectionStringOverride :  $"Server={Host}; Port=3306; Database={Database}; Uid={Username}; Pwd={Password};";
    }
}
