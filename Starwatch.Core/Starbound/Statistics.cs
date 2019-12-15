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
using Starwatch.Entities;
using Starwatch.Monitoring;
using System;
using System.Linq;
using static Starwatch.Starbound.Server;

namespace Starwatch.Starbound
{
    public struct Statistics
    {
        public int Connections { get; set; }
        public int? LastConnectionID { get; set; }
        public MemoryUsage MemoryUsage { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public double Uptime { get; set; }
        public long UptimeId { get; set; }
        public int TotalAccounts { get; set; }

        public string LastShutdownReason { get; set; }

        internal Statistics(Server server, MemoryUsage usage)
        {
            LastShutdownReason = server.LastShutdownReason;

            Connections = server.Connections.Count;
            LastConnectionID = server.Connections.LastestPlayer?.Connection;
            StartTime = server.StartTime;
            EndTime = server.EndTime;

            var uptimeMonitor = server.GetMonitors<UptimeMonitor>().FirstOrDefault();
            UptimeId = uptimeMonitor != null ? uptimeMonitor.CurrentUptimeId : 0;

            TotalAccounts = Account.LoadAllActiveAsync(server.DbContext).Result.Count;

            Uptime = 0;
            if (EndTime > StartTime) { Uptime = -(DateTime.UtcNow - EndTime).TotalSeconds; }
            if (StartTime > EndTime) { Uptime = (DateTime.UtcNow - StartTime).TotalSeconds; }
            MemoryUsage = usage;
        }
    }
}
