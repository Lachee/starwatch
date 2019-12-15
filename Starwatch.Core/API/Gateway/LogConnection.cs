/*
START LICENSE DISCLAIMER
Starwatch is a Starbound Server manager with player management, crash recovery and a REST and websocket (live) API. 
Copyright(C) 2020 Lachee

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published
by the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with this program. If not, see < https://www.gnu.org/licenses/ >.
END LICENSE DISCLAIMER
*/
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
