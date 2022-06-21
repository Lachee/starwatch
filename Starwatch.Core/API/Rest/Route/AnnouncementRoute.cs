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

using System;
using Starwatch.API.Rest.Routing;
using Starwatch.API.Rest.Route.Entities;
using Starwatch.API.Web;
using System.Linq;
using Starwatch.Monitoring;

namespace Starwatch.API.Rest.Route
{
    [Route("/announcement", AuthLevel.Admin)]
    class AnnouncementRoute : RestRoute
    {
        const string ResourceLocked   = "Resource is currently locked. Please wait and try again.";
        const string NoMonitor        = "AnnouncementMonitor was not found.";
        const string MissingResource  = "No such announcement.";
        const string IdMustBeInteger  = "id must be an integer.";
        const string IdMustBePositive = "id must be greater than or equal to 0.";

        public AnnouncementRoute(RestHandler handler, Authentication authentication) : base(handler, authentication) { }
        public override Type PayloadType => typeof(AnnouncementPatch);

        /// <summary>
        /// Gets the account exists and its admin state.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public override RestResponse OnGet(Query query)
        {
            // Grab the configurator from the monitor
            bool hasMon = Starbound.TryGetMonitor(out AnnouncementMonitor mon);

            if (!hasMon)
                return new RestResponse(RestStatus.InternalError, "AnnouncementMonitor was not found.");

            var announcements = mon.Announcements;

            if (query.ContainsKey("id"))
            {
                // Grabs a specific announcement.
                bool ok = int.TryParse(query["id"], out int id);

                if (!ok)
                    return new RestResponse(RestStatus.BadRequest, IdMustBeInteger);

                if (id < 0)
                    return new RestResponse(RestStatus.BadRequest, IdMustBePositive);

#pragma warning disable IDE0046 // Convert to conditional expression
                if (announcements.Length > id)
                    return new RestResponse(RestStatus.OK, announcements[id]);
#pragma warning restore IDE0046 // Convert to conditional expression

                return new RestResponse(RestStatus.ResourceNotFound, MissingResource);
            }
            else
            {
                // Returns all announcements
                return announcements.Length == 1
                    ? new RestResponse(RestStatus.OK,  "There is 1 announcement.", announcements)
                    : new RestResponse(RestStatus.OK, $"There are {announcements.Length} announcements.", announcements);
            }
        }

        /// <summary>
        /// Deletes the account
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public override RestResponse OnDelete(Query query)
        {
            // Grab the configurator from the monitor
            bool hasMon = Starbound.TryGetMonitor(out AnnouncementMonitor mon);

            if (!hasMon)
            {
                return new RestResponse(RestStatus.InternalError, NoMonitor);
            }

            if (query.ContainsKey("id"))
            {
                bool ok = int.TryParse(query["id"], out int id);

                if (!ok)
                    return new RestResponse(RestStatus.BadRequest, IdMustBeInteger);

                if (id < 0)
                    return new RestResponse(RestStatus.BadRequest, IdMustBePositive);

                if (!mon.Lock())
                    return new RestResponse(RestStatus.InternalError, ResourceLocked);

                if (mon.Announcements.Length > id)
                {
                    try
                    {
                        AnnouncementMonitor.Announcement target = mon.Announcements[id];

                        mon.Announcements = mon.Announcements.Where((s, i) => i != id).ToArray();
                        mon.Configuration.SetKey("announcements", mon.Announcements);
                        mon.Configuration.Save();

                        mon.Unlock();
                        mon.Reload();

                        return new RestResponse(RestStatus.OK, $"Deleted announcement #{id}", target);
                    }
                    catch (Exception ex)
                    {
                        mon.Unlock();
                        return new RestResponse(RestStatus.InternalError, $"Failed to save config: {ex.Message}");
                    }
                }

                mon.Unlock();
                return new RestResponse(RestStatus.ResourceNotFound, MissingResource);
            }
            else
            {
                return new RestResponse(RestStatus.BadRequest, "id must be included.");
            }
        }

        /// <summary>
        /// Updates the announcement
        /// </summary>
        /// <param name="query"></param>
        /// <param name="payloadObject"></param>
        /// <returns></returns>
        public override RestResponse OnPatch(Query query, object payloadObject)
        {
            if (payloadObject is null)
                return new RestResponse(RestStatus.BadRequest, "No payload.");

            var payload = (AnnouncementPatch) payloadObject;

            if (payload.Id < 0)
                return new RestResponse(RestStatus.BadRequest, IdMustBePositive);

            // Grab the configurator from the monitor
            bool hasMon = Starbound.TryGetMonitor(out AnnouncementMonitor mon);

            if (!hasMon)
                return new RestResponse(RestStatus.InternalError, NoMonitor);
            
            if (!mon.Lock())
                return new RestResponse(RestStatus.InternalError, ResourceLocked);
            

            AnnouncementMonitor.Announcement obj = null;

            if (!(mon.Announcements.Length > payload.Id))
            {
                mon.Unlock();
                return new RestResponse(RestStatus.BadRequest, "Announcement with that id does not exist.");
            }

            obj = mon.Announcements[payload.Id];

            var oa = (AnnouncementPatch)payloadObject;

            string message  = oa.Message  ?? obj.Message;
            bool enabled    = oa.Enabled  ?? obj.Enabled;
            double interval = oa.Interval ?? obj.Interval;

            var newObj = new AnnouncementMonitor.Announcement
            {
                Message = message,
                Enabled = enabled,
                Interval = interval
            };

            mon.Announcements[payload.Id] = newObj;

            try
            {
                mon.Configuration.SetKey("announcements", mon.Announcements);
                mon.Configuration.Save();
            }
            catch (Exception ex)
            {
                mon.Unlock();
                return new RestResponse(RestStatus.InternalError, $"Failed to save config: {ex.Message}");
            }

            mon.Unlock();
            mon.Reload();
            return new RestResponse(RestStatus.OK, $"Updated announcement #{payload.Id}", newObj);
        }

        public override RestResponse OnPost(Query query, object payloadObject)
        {
            if (payloadObject is null)
                return new RestResponse(RestStatus.BadRequest, "No payload.");

            var obj = (AnnouncementPatch)payloadObject;

            // Grab the configurator from the monitor
            bool hasMon = Starbound.TryGetMonitor(out AnnouncementMonitor mon);

            if (!hasMon)
                return new RestResponse(RestStatus.InternalError, NoMonitor);

            if (!mon.Lock())
                return new RestResponse(RestStatus.InternalError, ResourceLocked);

            var announcements = new AnnouncementMonitor.Announcement[mon.Announcements.Length + 1];

            for (int i = 0; i < mon.Announcements.Length; i++)
            {
                announcements[i] = mon.Announcements[i];
            }

            announcements[announcements.Length - 1] = obj.ToAnnouncement();

            mon.Announcements = announcements;
            mon.Configuration.SetKey("announcements", announcements);

            try
            {
                mon.Configuration.Save();
            }
            catch (Exception ex)
            {
                mon.Unlock();
                return new RestResponse(RestStatus.InternalError, $"Failed to save config: {ex.Message}");
            }

            mon.Unlock();
            mon.Reload();

            return new RestResponse(RestStatus.OK, "Added new announcement.");
        }
    }
}
