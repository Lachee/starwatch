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
using Starwatch.Entities;
using Starwatch.API.Rest.Route.Entities;
using Starwatch.API.Rest.Serialization;
using Starwatch.API.Web;
using Starwatch.API.Gateway.Event;
using Starwatch.API.Gateway;
using System.Linq;
using System.Threading.Tasks;
using Starwatch.Monitoring;
using Starwatch.Util;
using System.Diagnostics;

namespace Starwatch.API.Rest.Route
{
    [Route("/announcement", AuthLevel.Admin)]
    class AnnouncementRoute : RestRoute
    {
        const string RESOURCE_LOCKED = "Resource is currently locked. Please wait and try again.";
        const string NO_MONITOR = "AnnouncementMonitor was not found.";
        const string MISSING_RESOURCE = "No such announcement.";

        const string ID_MUST_BE_INTEGER = "id must be an integer.";
        const string ID_MUST_BE_POSITIVE = "id must be greater than or equal to 0.";

        public AnnouncementRoute(RestHandler handler, Authentication authentication) : base(handler, authentication) { }
        public override Type PayloadType => typeof(AnnouncementPatch);

        /// <summary>
        /// Gets the account exists and its admin state.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public override RestResponse OnGet(Query query)
        {
#if DEBUG
            if (Debugger.IsAttached)
            {
                if (query.ContainsKey("dbg"))
                {
                    if (query["dbg"] == "post")
                    {
                        return OnPost(query, null);
                    }

                    if (query["dbg"] == "delete")
                    {
                        return OnDelete(query);
                    }

                    if (query["dbg"] == "patch")
                    {
                        return OnPatch(query, null);
                    }
                }
            }
#endif
            // Grab the configurator from the monitor 
            AnnouncementMonitor mon = null;
            bool hasMon = Starbound.TryGetMonitor(out mon);

            if (!hasMon)
            {
                return new RestResponse(RestStatus.InternalError, "AnnouncementMonitor was not found.");
            }

            var announcements = mon.Announcements;

            if (query.ContainsKey("id"))
            {
                // Grabs a specific announcement.
                int id = 0;
                bool ok = int.TryParse(query["id"], out id);
                if (!ok)
                {
                    return new RestResponse(RestStatus.BadRequest, ID_MUST_BE_INTEGER);
                }

                if (id < 0)
                {
                    return new RestResponse(RestStatus.BadRequest, ID_MUST_BE_POSITIVE);
                }

                if (announcements.Length > id)
                {
                    return new RestResponse(RestStatus.OK, announcements[id]);
                }

                return new RestResponse(RestStatus.ResourceNotFound, MISSING_RESOURCE);
            }
            else
            {
                // Returns all announcements
                if (announcements.Length == 1)
                {
                    return new RestResponse(RestStatus.OK, "There is 1 announcement.", 1);
                }

                return new RestResponse(RestStatus.OK, $"There are {announcements.Length} announcements.", announcements);
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
            AnnouncementMonitor mon = null;
            bool hasMon = Starbound.TryGetMonitor(out mon);

            if (!hasMon)
            {
                return new RestResponse(RestStatus.InternalError, NO_MONITOR);
            }

            if (query.ContainsKey("id"))
            {
                int id = 0;
                bool ok = int.TryParse(query["id"], out id);

                if (!ok)
                {
                    return new RestResponse(RestStatus.BadRequest, ID_MUST_BE_INTEGER);
                }

                if (id < 0)
                {
                    return new RestResponse(RestStatus.BadRequest, ID_MUST_BE_POSITIVE);
                }

                if (!mon.Lock())
                {
                    return new RestResponse(RestStatus.InternalError, RESOURCE_LOCKED);
                }

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
                        return new RestResponse(RestStatus.InternalError, "Failed to save config: " + ex.Message);
                    }
                }

                mon.Unlock();
                return new RestResponse(RestStatus.ResourceNotFound, MISSING_RESOURCE);
            }
            else
            {
                return new RestResponse(RestStatus.BadRequest, "id must be included.");
            }
        }

#if DEBUG
        private AnnouncementPatch FakeBody (Query query)
        {
            if (!Debugger.IsAttached)
            {
                // shouldn't get here.
                throw new Exception("Phishing attempt.");
            }

            AnnouncementPatch ap = new AnnouncementPatch
            {
                Message = "",
                Enabled = false,
                Interval = 1800
            };

            if (query.ContainsKey("message"))
            {
                ap.Message = query["message"];
            }

            if (query.ContainsKey("enabled"))
            {
                bool e = false;
                if (bool.TryParse(query["enabled"], out e))
                {
                    ap.Enabled = e;
                }
            }

            if (query.ContainsKey("interval"))
            {
                double interval2 = 0d;
                if (double.TryParse(query["interval"], out interval2))
                {
                    ap.Interval = interval2;
                }
            }

            return ap;
        }
#endif

        /// <summary>
        /// Updates the announcement
        /// </summary>
        /// <param name="query"></param>
        /// <param name="payloadObject"></param>
        /// <returns></returns>
        public override RestResponse OnPatch(Query query, object payloadObject)
        {
#if DEBUG
            if (Debugger.IsAttached)
            {
                if (payloadObject == null)
                {
                    payloadObject = FakeBody(query);
                }
            }
#endif

            if (payloadObject == null)
            {
                return new RestResponse(RestStatus.BadRequest, "No payload.");
            }

            AnnouncementPatch payload = (AnnouncementPatch) payloadObject;

            if (payload.Id < 0)
            {
                return new RestResponse(RestStatus.BadRequest, ID_MUST_BE_POSITIVE);
            }

            // Grab the configurator from the monitor 
            AnnouncementMonitor mon = null;
            bool hasMon = Starbound.TryGetMonitor(out mon);

            if (!hasMon)
            {
                return new RestResponse(RestStatus.InternalError, NO_MONITOR);
            }

            if (!mon.Lock())
            {
                return new RestResponse(RestStatus.InternalError, RESOURCE_LOCKED);
            }

            AnnouncementMonitor.Announcement obj = null;

            if (!(mon.Announcements.Length > payload.Id))
            {
                mon.Unlock();
                return new RestResponse(RestStatus.BadRequest, "Announcement with that id does not exist.");
            }

            obj = mon.Announcements[payload.Id];

            AnnouncementPatch oa = (AnnouncementPatch)payloadObject;

            string message = oa.Message ?? obj.Message;
            bool enabled = oa.Enabled ?? obj.Enabled;
            double interval = oa.Interval ?? obj.Interval;

            AnnouncementMonitor.Announcement newObj = new AnnouncementMonitor.Announcement
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
                return new RestResponse(RestStatus.InternalError, "Failed to save config: " + ex.Message);
            }

            mon.Unlock();
            mon.Reload();
            return new RestResponse(RestStatus.OK, $"Updated announcement #{payload.Id}", newObj);
        }

        public override RestResponse OnPost(Query query, object payloadObject)
        {
#if DEBUG
            if (Debugger.IsAttached)
            {
                if (payloadObject == null)
                {
                    payloadObject = FakeBody(query);
                }
            }
#endif

            if (payloadObject == null)
            {
                return new RestResponse(RestStatus.BadRequest, "No payload.");
            }

            AnnouncementPatch obj = (AnnouncementPatch)payloadObject;

            // Grab the configurator from the monitor 
            AnnouncementMonitor mon = null;
            bool hasMon = Starbound.TryGetMonitor(out mon);

            if (!hasMon)
            {
                return new RestResponse(RestStatus.InternalError, NO_MONITOR);
            }

            if (!mon.Lock())
            {
                return new RestResponse(RestStatus.InternalError, RESOURCE_LOCKED);
            }

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
                return new RestResponse(RestStatus.InternalError, "Failed to save config: " + ex.Message);
            }

            mon.Unlock();
            mon.Reload();

            return new RestResponse(RestStatus.OK, "Added new announcement.");
        }
    }
}
