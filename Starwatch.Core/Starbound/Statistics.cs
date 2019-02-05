using System;
using System.Collections.Generic;
using System.Text;

namespace Starwatch.Starbound
{
    public struct Statistics
    {
        public int Connections { get; set; }
        public int? LastConnectionID { get; set; }
        public long MemoryUsage { get; set; }
        public long PeakUsage { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public double Uptime { get; set; }

        public long CurrentBanTicket { get; set; }
        public int TotalAccounts { get; set; }

        internal Statistics(Server server)
        {
            Connections = server.Connections.Count;
            LastConnectionID = server.Connections.LastestPlayer?.Connection;
            StartTime = server.StartTime;
            EndTime = server.EndTime;

            TotalAccounts = server.Settings.Accounts.Accounts.Count;
            CurrentBanTicket = server.Settings.CurrentBanTicket;

            Uptime = 0;
            if (EndTime > StartTime) { Uptime = -(DateTime.UtcNow - EndTime).TotalSeconds; }
            if (StartTime > EndTime) { Uptime = (DateTime.UtcNow - StartTime).TotalSeconds; }

            MemoryUsage = server.GetMemoryUsage();
            PeakUsage = server.GetPeakMemoryUsage();
        }
    }
}
