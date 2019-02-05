using System;
using System.Collections.Generic;
using System.Text;

namespace Starwatch.API.Gateway.Models
{
    public enum OpCode : byte
    {
        Close        = 0,
        Hello        = 1,
        Welcome      = 2,
        Filter       = 3,
        FilterAck    = 4,
        Heartbeat    = 5,
        HeartbeatAck = 6,

        LogEvent     = 10,
        ServerEvent  = 12,
        PlayerEvent  = 14
    }
}
