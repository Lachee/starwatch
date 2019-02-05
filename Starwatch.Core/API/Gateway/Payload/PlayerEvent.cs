using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using Starwatch.API.Gateway.Models;
using Starwatch.Entities;

namespace Starwatch.API.Gateway.Payload
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    class PlayerEvent : IPayload
    {
        public const string EVENT_CONNECT = "CONN";
        public const string EVENT_DISCONNECT = "DISC";
        public const string EVENT_UPDATE = "UPDT";
        public const string EVENT_SYNC = "SYNC";

        OpCode IPayload.OpCode => OpCode.PlayerEvent;
        string IPayload.Identifier => Event;
        object IPayload.Data => Player;
        
        public string Event { get; set; }
        public Player Player { get; set; }
    }
}
