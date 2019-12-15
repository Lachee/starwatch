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
using Starwatch.API.Web;
using Starwatch.Entities;
using Starwatch.API.Rest.Serialization;
using System.Threading.Tasks;
using Starwatch.Starbound.Rcon;
using Starwatch.API.Gateway;
using Starwatch.API.Gateway.Event;

namespace Starwatch.API.Rest.Route
{
    [Route("/player/:cid", AuthLevel.Admin)]
    class PlayerDetailsRoute : RestRoute, IGatewayRoute
    {
        [Argument("cid", Converter = typeof(ConnectionConverter))]
        public Player Player { get; internal set; }
        
        public PlayerDetailsRoute(RestHandler handler, Authentication authentication) : base(handler, authentication) { }
        public PlayerDetailsRoute(Player player) : this(null, null)
        {
            this.Player = player;
        }
        
        public string GetRouteName() => "/player/:cid";
        public void SetGateway(EventConnection jsonConnection)
        {
            this.Handler = jsonConnection.API.RestHandler;
            this.Authentication = jsonConnection.Authentication;
        }
        
        public override RestResponse OnGet(Query query)
        {
            //Make sure the player exists
            if (Player == null) return RestResponse.ResourceNotFound;

            //The user hasn't gotten a UUID yet, we will force a refresh.
            if (!query.GetBool("skip_list", false) && Player.UUID == null)
                Starbound.Connections.RefreshListing().Wait();

            //We are enforcing a valid location, so refresh their location.
            if (query.GetBool("enforce", false))
                Starbound.Connections.RefreshLocation(Player).Wait();            

            //Return the response
            return new RestResponse(RestStatus.OK, res: Player);
        }

        public override RestResponse OnDelete(Query query)
        {
            if (Player == null) return RestResponse.ResourceNotFound;

            string reason;
            int duration = 0;
            bool async = query.GetBool(Query.AsyncKey, false);

            if (Starbound.Rcon == null)
                return new RestResponse(RestStatus.BadRequest, "Rcon not enabled.");

            //Get the reason
            if (!query.TryGetString("reason", out reason))
                return new RestResponse(RestStatus.BadRequest, msg: "Cannot delete user without a reason");

            //Prepare the task
            Task<RconResponse> task;
            
            //If we have a duration then kick for the duration
            if (query.TryGetInt("duration", out duration))
            {
                task = Starbound.Kick(Player, reason, duration);
            }
            else
            {
                task = Starbound.Kick(Player, reason);
            }

            //If we are async then return asap, otherwise get the result
            if (async) return RestResponse.Async;
            return new RestResponse(RestStatus.OK, res: task.Result);
        }

    }
}
