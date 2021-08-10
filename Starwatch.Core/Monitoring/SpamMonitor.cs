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

        /// <summary>
        /// The weight at which the spam will trigger
        /// </summary>
        public int TriggerThreshold { get; private set; }

        /// <summary>
        /// How heavy repeated messages are.
        /// </summary>
        public int Weight { get; private set; } = 3;

        /// <summary>
        /// List of tallies
        /// </summary>
        private Dictionary<string, int> _tallies = new Dictionary<string, int>();

        /// <summary>
        /// Disable Anonymous Connections on spam detection
        /// </summary>
        public bool DisableAnonymousConnections { get; private set; }

        /// <summary>
        /// Restart the server on spam detection.
        /// </summary>
        public bool RestartServer { get; private set; }

        public SpamMonitor(Server server) : base(server, "SpamMonitor") { }

        public override Task Initialize()
        {
            TriggerThreshold            = Configuration.GetInt("theshold", 10) * Weight;
            DisableAnonymousConnections = Configuration.GetBool("disable_anonymous", true);
            RestartServer               = Configuration.GetBool("restart_server", true);
            return Task.CompletedTask;
        }

        public override async Task<bool> HandleMessage(Message msg)
        {
            return false;
            /*
            //Clean tallies then try to increment.
            CleanTallies();
            int total = IncrementTally(msg.Content);
            if (total >= TriggerThreshold)
            {
                //Disable anonymous
                Logger.Log("Premptive Attack Detected!");
                if (DisableAnonymousConnections)
                {
                    Server.Configurator.AllowAnonymousConnections = false;
                    await Server.Configurator.SaveAsync(false);
                }

                //Report the crash to the gateway
                Server.ApiHandler.BroadcastRoute((gateway) =>
                {
                    if (gateway.Authentication.AuthLevel < API.AuthLevel.Admin) return null;
                    return msg;
                }, "OnSpam");

                //Restart
                if (RestartServer)
                {
                    throw new ServerShutdownException("Premptive Attack Mitigation: " + msg.Content);
                }
            }

            return false;*/
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
