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
using Starwatch.Entities;
using Starwatch.Exceptions;
using Starwatch.Logging;
using Starwatch.Starbound;

namespace Starwatch.Monitoring
{
    class SegfaultMonitor : Monitor
	{
        const string FATAL_ERROR = "Fatal Error: ";

        public SegfaultMonitor(Server server) : base(server, "Segfault") { }

        public override int Priority => 50;

        public override Task<bool> HandleMessage(Message msg)
		{
            //We only care for error messages, which we will use to find segfaults.
            if (msg.Level == Message.LogLevel.Error)
            {
                if (msg.Content.StartsWith(FATAL_ERROR))
                {
                    Logger.LogError("Fatal error has occured: " + msg);

                    //Send a API log to the error
                    Server.ApiHandler.BroadcastRoute((gateway) =>
                    {
                        if (gateway.Authentication.AuthLevel < API.AuthLevel.Admin) return null;
                        return msg;
                    }, "OnSegfaultCrash");

                    //Throw the error, causing everything to abort
                    throw new ServerShutdownException("Fatal Exception: " + msg);
                }
            }

			return Task.FromResult(false);
		}

	}
}
