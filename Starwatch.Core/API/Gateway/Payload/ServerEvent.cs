using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using Starwatch.API.Gateway.Models;
using Starwatch.Entities;

namespace Starwatch.API.Gateway.Payload
{
    class ServerEvent : IPayload
    {
        public const string EVENT_START = "STRT";
        public const string EVENT_EXIT = "EXIT";
        public const string EVENT_RELOAD = "LOAD";

        OpCode IPayload.OpCode => OpCode.ServerEvent;
        string IPayload.Identifier => Event;
        object IPayload.Data => Reason;
        
        public string Event { get; set; }
        public string Reason { get; set; }
    }
}
