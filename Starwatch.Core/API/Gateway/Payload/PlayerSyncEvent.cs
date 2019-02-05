using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using Starwatch.API.Gateway.Models;
using Starwatch.Entities;

namespace Starwatch.API.Gateway.Payload
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    class PlayerSyncEvent : IPayload
    {
        OpCode IPayload.OpCode => OpCode.PlayerEvent;
        string IPayload.Identifier => PlayerEvent.EVENT_SYNC;
        object IPayload.Data => Players;        
        public Player[] Players { get; set; }
    }
}
