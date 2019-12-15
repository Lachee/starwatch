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
using Starwatch.Database;
using Starwatch.Entities;
using Starwatch.Logging;
using Starwatch.Starbound;
using Starwatch.Util;

namespace Starwatch.Monitoring
{
    public abstract class Monitor
    {
        public DbContext DbContext => Server.DbContext;
        public Server Server { get; }
		public Logger Logger { get; }
		public string Name { get; }

        public virtual int Priority => 100;

        public Monitor(Server server)
        {
            this.Server = server;
            this.Name = this.GetType().FullName;
            this.Logger = new Logger(Name.ToUpperInvariant(), Server.Logger);
        }

		public Monitor(Server server, string name)
		{
			this.Server = server;
			this.Name = name;
			this.Logger = new Logger(name.ToUpperInvariant(), Server.Logger);
        }

        public virtual Task Initialize() => Task.CompletedTask;
        public virtual Task Deinitialize() => Task.CompletedTask;

        /// <summary>
        /// Called after the settings are loaded but before they are saved again and the server is started.
        /// </summary>
        /// <returns></returns>
        public virtual Task OnServerPreStart() => Task.CompletedTask;

        /// <summary>
        /// Called after the server starts but before the first line of input.
        /// </summary>
        /// <returns></returns>
        public virtual Task OnServerStart() => Task.CompletedTask;

        /// <summary>
        /// Called when ever the server exits.
        /// </summary>
        /// <returns></returns>
        public virtual Task OnServerExit(string reason) => Task.CompletedTask;

        public virtual Task<bool> HandleMessage(Message msg) => Task.FromResult(false);


        public virtual void Dispose() { }
    }

    public abstract class ConfigurableMonitor : Monitor
    {
        public Configuration Configuration { get; }
        public ConfigurableMonitor(Server server) : base(server) { this.Configuration = server.Configuration.GetConfiguration(Name); }
        public ConfigurableMonitor(Server server, string name) : base(server, name) { this.Configuration = server.Configuration.GetConfiguration(Name); }
    
    }
}
