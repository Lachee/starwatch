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
using System;
using System.Linq;
using System.Threading.Tasks;
using Starwatch.Entities;
using Starwatch.Exceptions;
using Starwatch.Logging;
using Starwatch.Starbound;
using Starwatch.Util;

namespace Starwatch.Monitoring
{
    class UptimeMonitor : Monitor
    {
        public long CurrentUptimeId => CurrentUptime != null ? CurrentUptime.Id : 0;
        public Uptime CurrentUptime { get; private set; }

        public override int Priority => 11;

        private DropoutStack<string> _stack;
        public UptimeMonitor(Server server) : base(server, "Uptime")
        {
            _stack = new DropoutStack<string>(10);
        }

        public override async Task OnServerStart()
        {
            //If the last one was left open, we will just close it again (just in case)
            if (CurrentUptime != null)
                await StopUptimeAsync(Server.LastShutdownReason);
            
            //Clear any uptime that hasn't been cleared yet
            await StopAllUptimeAsync();

            //Set our uptime
            await StartUptimeAsync();
        }

        public override async Task OnServerExit(string reason)
        {
            //Stop the uptime
            await StopUptimeAsync(reason);
        }

        public async Task StartUptimeAsync()
        {
            _stack.Clear();
            CurrentUptime = new Uptime();
            await CurrentUptime.SaveAsync(DbContext);
        }

        public async Task StopUptimeAsync(string reason)
        {
            if (CurrentUptime == null)
                return;

            CurrentUptime.Ended = DateTime.UtcNow;
            CurrentUptime.LastLog = string.Join("\n", _stack);
            CurrentUptime.Reason = reason;
            await CurrentUptime.SaveAsync(DbContext);

            CurrentUptime = null;
        }

        public Task<int> StopAllUptimeAsync() => Uptime.EndAllAsync(DbContext);

        public override Task<bool> HandleMessage(Message msg)
		{
            //Add the logs to the stack
            _stack.Push(msg.ToString());
            return Task.FromResult(false);
		}


	}
}
