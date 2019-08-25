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

        [JsonIgnore]
        public string ConnectionString => $"Server={Host}; Port=3306; Database={Database}; Uid={Username}; Pwd={Password};";
    }
}
