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
using Newtonsoft.Json;
using Starwatch.Entities;
using Starwatch.Monitoring;
using System;

namespace Starwatch.API.Rest.Route.Entities
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    struct AnnouncementPatch
    {
        public int Id { get; set; }
        public string Message { get; set; }
        public double? Interval { get; set; }
        public bool? Enabled { get; set; }

        public AnnouncementMonitor.Announcement ToAnnouncement()
        {
            return new AnnouncementMonitor.Announcement
            {
                Message = Message,
                Interval = Interval ?? 1800d,
                Enabled = Enabled ?? false
            };
        }
    }
}
