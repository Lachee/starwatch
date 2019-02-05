using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using Starwatch.API.Gateway.Models;

namespace Starwatch.API.Gateway.Payload
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    class Welcome : IPayload
    {

        OpCode IPayload.OpCode => OpCode.Welcome;
        string IPayload.Identifier => "WELC";
        object IPayload.Data => this;

        [JsonProperty]
        public long Connection { get; set; }
       
        [JsonProperty]
        public string ID { get; set; }

        [JsonProperty]
        public string Agent { get; set; }
    }
}
