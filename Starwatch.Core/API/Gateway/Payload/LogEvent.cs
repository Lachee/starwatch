using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using Starwatch.API.Gateway.Models;
using Starwatch.Entities;

namespace Starwatch.API.Gateway.Payload
{
    class LogEvent : IPayload
    {

        public const string EVENT_INFO      = "INFO";
        public const string EVENT_WARNING   = "WARN";
        public const string EVENT_ERROR     = "ERRO";
        public const string EVENT_CHAT      = "CHAT";

        OpCode IPayload.OpCode => OpCode.LogEvent;
        string IPayload.Identifier => GetMessageIdentifier();
        object IPayload.Data => Message;

        public Message Message;
        private string GetMessageIdentifier()
        {
            switch(Message.Level)
            {
                default:
                case Message.LogLevel.Info:
                    return EVENT_INFO;

                case Message.LogLevel.Warning:
                    return EVENT_WARNING;

                case Message.LogLevel.Error:
                    return EVENT_ERROR;

                case Message.LogLevel.Chat:
                    return EVENT_CHAT;
            }
        }
    }
}
