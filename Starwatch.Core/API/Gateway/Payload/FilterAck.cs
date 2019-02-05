using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using Starwatch.API.Gateway.Models;

namespace Starwatch.API.Gateway.Payload
{
    class FilterAck : IPayload
    {

        OpCode IPayload.OpCode => OpCode.FilterAck;
        string IPayload.Identifier => "SMRY";
        object IPayload.Data => Summary;

        public Filter Summary { get; set; }
    }
}
