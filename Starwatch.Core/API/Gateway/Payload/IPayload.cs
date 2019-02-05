using System;
using System.Collections.Generic;
using System.Text;
using Starwatch.API.Gateway.Models;

namespace Starwatch.API.Gateway.Payload
{
    public interface IPayload
    {
        OpCode OpCode { get; }
        string Identifier { get; }
        object Data { get; }
    }
}
