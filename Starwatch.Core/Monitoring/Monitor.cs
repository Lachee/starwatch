using System.Threading.Tasks;
using Starwatch.Entities;
using Starwatch.Logging;
using Starwatch.Starbound;
using Starwatch.Util;

namespace Starwatch.Monitoring
{
    public abstract class Monitor
	{
		public Server Server { get; }
		public Logger Logger { get; }
		public string Name { get; }

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
