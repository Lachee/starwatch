using Newtonsoft.Json;
using Starwatch.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Starwatch.API
{
    public class BotAccount : Account
    {
        [JsonProperty("redirect_url")]
        public string AuthRedirect;
        public BotAccount(string name) : base(name) { }
    }
}
