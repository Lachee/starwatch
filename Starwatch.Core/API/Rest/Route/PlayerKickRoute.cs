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
using Starwatch.API.Rest.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using Starwatch.API.Web;
using Starwatch.Entities;
using Starwatch.Extensions;

namespace Starwatch.API.Rest.Route
{
    [Route("/player/kick", AuthLevel.Admin)]
    class PlayerKickRoute : RestRoute
    {
        private Logging.Logger logger { get; set; }

        class PlayerKickResponse
        {
            public List<Player> Players { get; set; } = new List<Player>();
            public int SuccessfulKicks { get; set; } = 0;
            public int FailedKicks { get; set; } = 0;
        }

        public PlayerKickRoute(RestHandler handler, Authentication authentication) : base(handler, authentication)
        {
            logger = new Logging.Logger("SW-ROUTE-KICK");
        }

        public string GetRouteName() => "/player/kick";

        public override RestResponse OnDelete(Query query)
        {
            IEnumerable<Player> res = Starbound.Connections.GetPlayersEnumerable();

            string reason = query.GetString("reason", "Disconnected due to inactivity");
            bool async = query.GetBool("async", false);

            PlayerKickResponse response = new PlayerKickResponse();
            bool performedQuery = false;
            int? duration = null;

            int nDuration = query.GetInt("duration", -1);

            if (nDuration != -1)
            {
                duration = nDuration;
                reason += $"\nTimeout for {duration} seconds.";
            }

            if (query.ContainsKey("ip"))
            {
                res = res.Where(x => 
                    x.IP != null &&
                    x.IP.Equals(query.GetString("ip", "")));

                if (res.Count() == 0)
                    return new RestResponse(RestStatus.ResourceNotFound, $"No user with IP address '{query.GetString("ip", "")}' was found.");

                performedQuery = true;
            }

            if (query.ContainsKey("uuid"))
            {
                res = res.Where(x => 
                    x.UUID != null && 
                    x.UUID.Equals(query.GetString("uuid", ""), StringComparison.InvariantCultureIgnoreCase));

                if (res.Count() == 0)
                    return new RestResponse(RestStatus.ResourceNotFound, $"No user with UUID '{query.GetString("uuid", "-")}' was found.");

                performedQuery = true;
            }

            if (query.ContainsKey("username"))
            {
                res = res.Where(x =>
                    x.Username != null &&
                    x.Username.Equals(query.GetString("username", ""), StringComparison.InvariantCultureIgnoreCase));

                if (res.Count() == 0)
                    return new RestResponse(RestStatus.ResourceNotFound, $"No user with username '{query.GetString("username", "")}' was found.");

                performedQuery = true;
            }
            
            if (query.ContainsKey("nickname"))
            {
                res = res.Where(x =>
                    x.Nickname != null &&
                    x.Nickname.Equals(query.GetString("nickname", ""), StringComparison.InvariantCultureIgnoreCase));

                if (res.Count() == 0)
                    return new RestResponse(RestStatus.ResourceNotFound, $"No user with nickname '{query.GetString("nickname", "-")}' was found.");

                performedQuery = true;
            }

            if (query.ContainsKey("account"))
            {
                res = res.Where(x =>
                    x.AccountName != null &&
                    x.AccountName.Equals(query.GetString("account", ""), StringComparison.InvariantCultureIgnoreCase));

                if (res.Count() == 0)
                    return new RestResponse(RestStatus.ResourceNotFound, $"No user with account name '{query.GetString("account", "")}' was found.");

                performedQuery = true;
            }

            if (query.ContainsKey("location"))
            {
                res = res.Where(x =>
                    x.Location != null &&
                    x.Location.Whereami.Equals(query.GetString("location", ""), StringComparison.InvariantCultureIgnoreCase));

                if (res.Count() == 0)
                    return new RestResponse(RestStatus.ResourceNotFound, $"No user with location '{query.GetString("location", "")}' was found.");

                performedQuery = true;
            }

            if (query.ContainsKey("cid"))
            {
                int cid = -1;

                if (!query.TryGetInt("cid", out cid))
                {
                    return new RestResponse(
                        RestStatus.BadRequest,
                        $"cid must be a valid integer. " +
                        $"Received '{query.GetString("cid", "")}'"
                    );
                }

                res = res.Where(x => x.Connection == cid);

                if (res.Count() == 0)
                    return new RestResponse(RestStatus.ResourceNotFound, $"No user with CID '{cid}' was found.");

                performedQuery = true;
            }

            if (!performedQuery)
            {
                return new RestResponse(
                    RestStatus.BadRequest,
                    $"Must provide a valid query parameter. Valid options include: " +
                    $"'ip', 'uuid', 'username', 'nickname', 'account', 'cid'."
                );
            }

            foreach (Player p in res)
            {
                if (async)
                    p.Kick(reason, duration).CallAsyncWithLog(logger, "Exception kicking user: {0}");

                else
                {
                    var rconRes = p.Kick(reason, duration).Result;

                    if (rconRes.Success)
                        response.SuccessfulKicks++;

                    else
                        response.FailedKicks++;
                }
            }

            return async
                ? new RestResponse(RestStatus.Async, $"Attempting to kick {res.Count()} players.", response)
                : new RestResponse(RestStatus.OK, $"Kicked {response.SuccessfulKicks} players successfully.", response);
        }

    }
}
