using Starwatch.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;

namespace Starwatch.API.Gateway
{
    class LogConnection : GatewayConnection
    {
        public override string ToString() => $"GatewayLog({Identifier})";

        protected override void OnMessage(MessageEventArgs msg)
        {
            //Make sure we are not terminated
            if (HasTerminated)
            {
                Logger.Log("Received a message from a terminated connection.");
                return;
            }

            //Validate the authentication
            if (Authentication == null && !ValidateAuthentication())
                return;

            //Make sure its binary
            if (!msg.IsBinary)
            {
                Logger.Log("Tried to get data but it wasnt sent in binary");
                Terminate(CloseStatusCode.UnsupportedData, "binary only");
                return;
            }
        }

        public void SendLog(string log)
        {
            //Validate the authentication
            if (Authentication == null && !ValidateAuthentication())
                return;

            //Send log
            _ = Task.Run(() => Send(log));
        }

    }
}
