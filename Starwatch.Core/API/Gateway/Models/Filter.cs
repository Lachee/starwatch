using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Starwatch.API.Gateway.Models
{
    public class Filter
    {
        [JsonProperty("PLYR")]
        public bool PlayerEvents { get; set; } = false;

        [JsonProperty("SERV")]
        public bool ServerEvents { get; set; } = false;

        [JsonProperty("LOGS")]
        public bool LogEvents { get; set; } = false;
    }
}
