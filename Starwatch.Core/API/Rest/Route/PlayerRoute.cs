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
using Starwatch.API.Rest.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using Starwatch.API.Web;

namespace Starwatch.API.Rest.Route
{
    [Route("/player")]
    class PlayerRoute : RestRoute
    {
        public PlayerRoute(RestHandler handler, Authentication authentication) : base(handler, authentication) { }
        public override RestResponse OnGet(Query query)
        {
            var enumerator = Handler.Starbound.Connections.GetCopiedPlayersEnumerable();

            string username, nickname, accountname, uuid, location;
            bool admin;

            //Filter the result
            if (query.TryGetString("uuid", out uuid)) enumerator = enumerator.Where(p => p.UUID != null && p.UUID.Equals(uuid, StringComparison.InvariantCultureIgnoreCase));
            if (query.TryGetString("username", out username)) enumerator = enumerator.Where(p => p.Username.Equals(username, StringComparison.InvariantCultureIgnoreCase));
            if (query.TryGetString("nickname", out nickname)) enumerator = enumerator.Where(p => p.Nickname.Equals(nickname, StringComparison.InvariantCultureIgnoreCase));
            if (query.TryGetString("location", out location)) enumerator = enumerator.Where(p => p.Location.Whereami.Equals(location, StringComparison.InvariantCultureIgnoreCase));
            if (query.TryGetString("account", out accountname))
            {
                enumerator = enumerator.Where(p => 
                        accountname.Equals("anonymous", StringComparison.InvariantCultureIgnoreCase) ? 
                            p.AccountName == null : 
                            p.AccountName.Equals(accountname, StringComparison.InvariantCultureIgnoreCase)
                    );
            }

            if (query.TryGetBool("admin", out admin))
                enumerator = enumerator.Where(p => p.AccountName != null && (p.GetAccountAsync().Result?.IsAdmin).GetValueOrDefault(false));

            //Prepare the array
            Dictionary<int, string> players = new Dictionary<int, string>();
            foreach (var p in enumerator) players.Add(p.Connection, p.Username);

            //Return the array
            return new RestResponse(RestStatus.OK, players);
        }
        
    }
}
