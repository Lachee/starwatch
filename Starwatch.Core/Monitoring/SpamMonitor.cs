using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Starwatch.Entities;
using Starwatch.Exceptions;
using Starwatch.Logging;
using Starwatch.Starbound;
using Starwatch.Util;

namespace Starwatch.Monitoring
{
    class SpamMonitor : ConfigurableMonitor
    {

        public override int Priority => 11;


        public int TriggerThreshold { get; }
        public int Weight { get; } = 3;

        private Dictionary<string, int> _tallies;


        public SpamMonitor(Server server) : base(server, "Spam")
        {
            TriggerThreshold = Configuration.GetInt("theshold", 10) * Weight;
            _tallies = new Dictionary<string, int>();
        }

		public override async Task<bool> HandleMessage(Message msg)
        {
            //Clean tallies then try to increment.
            CleanTallies();
            int total = IncrementTally(msg.Content);
            if (total >= TriggerThreshold)
            {
                //Disable anonymous
                Logger.Log("Premptive Attack Detected!");
                Server.Configurator.AllowAnonymousConnections = false;
                await Server.Configurator.SaveAsync(false);

                //Restart
                throw new ServerShutdownException("Premptive Attack Mitigation: " + msg.Content);
            }

            return false;
		}

        /// <summary>
        /// Increments the tally of the message
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        private int IncrementTally(string content)
        {
            string tolwr = content.ToLowerInvariant();
            if (!_tallies.ContainsKey(tolwr)) _tallies.Add(tolwr, 0);
            return (_tallies[tolwr] += Weight);
        }

        /// <summary>
        /// Clean all the empty references to the tallies
        /// </summary>
        private void CleanTallies()
        {
            //Prepare a list of removes
            List<string> removes = new List<string>(0);
            string[] keys = _tallies.Keys.ToArray();
            foreach (var key in keys)
            {
                //Decrement the tally, dropping if nessary
                _tallies[key] -= 1;
                if (_tallies[key] <= 0)
                    removes.Add(key);
            }

            //remove each tally that is 0
            foreach (var r in removes)
                _tallies.Remove(r);
        }
    }
}
