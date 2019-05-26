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
