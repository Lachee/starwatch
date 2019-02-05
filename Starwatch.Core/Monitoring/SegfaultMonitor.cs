using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Starwatch.Entities;
using Starwatch.Logging;
using Starwatch.Starbound;

namespace Starwatch.Monitoring
{
	class SegfaultMonitor : Monitor
	{
        const string FATAL_ERROR = "Fatal Error: ";

        public SegfaultMonitor(Server server) : base(server, "Segfault") { }

		public override Task<bool> HandleMessage(Message msg)
		{
            //We only care for error messages, which we will use to find segfaults.
            if (msg.Level == Message.LogLevel.Error)
            {
                if (msg.Content.StartsWith(FATAL_ERROR))
                {
                    Logger.LogError("Fatal error has occured: " + msg);
                    return Task.FromResult(true);
                }
            }

			return Task.FromResult(false);
		}

	}
}
