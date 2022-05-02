/*
START LICENSE DISCLAIMER
Starwatch is a Starbound Server manager with player management, crash recovery and a REST and websocket (live) API. 
Copyright(C) 2022 Lachee

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
using System.Timers;
using System.Threading.Tasks;
using Starwatch.Entities;
using Starwatch.Logging;
using Starwatch.Starbound;
using Starwatch.Util;

namespace Starwatch.Monitoring
{
    class AnnouncementMonitor : ConfigurableMonitor
    {
        public class Announcement
        {
            public string Message { get; set; } = "";
            public double Interval { get; set; } = 1800;
            public bool Enabled { get; set; } = false;
        }

        public override int Priority => 10;

        public bool EnableMonitor { get; set; }

        public Announcement[] Announcements = new Announcement[0];

        public Timer[] AnnouncementTimers { get; set; } = new Timer[0];

        private bool Locked { get; set; } = false;

        public AnnouncementMonitor(Server server) : base(server, "AnnouncementMonitor")
        {
            Logger.Colourise = false;
        }

        private void SendAnnouncement(int id)
        {
            if (Locked)
            {
                return;
            }

            var res = Server.Rcon.BroadcastAsync(Announcements[id].Message).Result;
            if (!res.Success)
            {
                Logger.LogError($"Failed to send announcement #{id}");
            }
        }

        public override Task Initialize()
        {
            EnableMonitor = Configuration.GetBool("enable_monitor", true);

            Announcement[] announcements = null;
            bool hasAnnouncements = Configuration.TryGetObject<Announcement[]>("announcements", out announcements);
            Announcements = announcements;

            if (!hasAnnouncements)
            {
                Announcements = new Announcement[]
                {
                    new Announcement
                    {
                        Message = "<^pink;server^reset;> Example Announcement every 60 seconds!",
                        Interval = 60,
                        Enabled = false
                    }
                };

                Configuration.SetKey("announcements", Announcements);
                Configuration.Save();
            }

            if (!EnableMonitor)
            {
                return Task.CompletedTask;
            }

            Reload();

            //Return the completed task
            return Task.CompletedTask;
        }

        public void Reload()
        {
            foreach (Timer timer in AnnouncementTimers)
            {
                if (timer is null)
                {
                    continue;
                }

                if (timer.Enabled)
                {
                    timer.Stop();
                    timer.Dispose();
                }
            }

            AnnouncementTimers = new Timer[Announcements.Length];

            for (int i = 0; i < Announcements.Length; i++)
            {
                int j = i;
                if (!Announcements[i].Enabled)
                {
                    AnnouncementTimers[i] = null;
                    continue;
                }

                AnnouncementTimers[i] = new Timer();
                AnnouncementTimers[i].Interval = Announcements[i].Interval * 1000d;
                AnnouncementTimers[i].Elapsed += (s, e) =>
                {
                    SendAnnouncement(j);
                };

                AnnouncementTimers[i].Start();
            }
        }

        public bool Lock()
        {
            if (Locked)
                return false;

            Locked = true;
            return true;
        }

        public void Unlock()
        {
            Locked = false;
        }
    }
}
