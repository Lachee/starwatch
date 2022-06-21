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
using System;
using Starwatch.API.Rest.Routing;
using Starwatch.Entities;
using Starwatch.API.Web;

namespace Starwatch.API.Rest.Route
{
    [Route("/ban", AuthLevel.Admin)]
    class BanRoute : RestRoute
    {
        public override Type PayloadType => typeof(Ban);

        public BanRoute(RestHandler handler, Authentication authentication) : base(handler, authentication) { }

        ///// <summary>
        ///// Gets the last ban
        ///// </summary>
        //public override RestResponse OnGet(Query query)
        //{
        //    return new RestResponse(RestStatus.OK, res: Starbound.Settings.CurrentBanTicket);
        //}

        /// <summary>
        /// Creates a new ban
        /// </summary>
        public override RestResponse OnPost(Query query, object payloadObject)
        {
            //Prepare some values
            var ban = (Ban)payloadObject;
            Player player = null;

            //Validate the ban exists
            if (ban.Ticket.HasValue)
                return new RestResponse(RestStatus.BadRequest, msg: "Bans cannot have their tickets preset!");

            //If we have a connection ID then we will fill the rest with the connection information.
            // otherwise we will just ban the player as usual.
            if (query.ContainsKey("cid"))
            {
                //We are banning a specific connection id.
                if (!query.TryGetInt("cid", out int connection))
                    return new RestResponse(RestStatus.BadRequest, msg: $"Cannot convert '{query["cid"]}' to an int32!");

                //Get the connection
                player = Starbound.Connections.GetPlayer(connection);

                //Make sure the player is upto date
                if (player != null && player.UUID == null)
                {
                    Starbound.Connections.RefreshListing().Wait();
                    player = Starbound.Connections.GetPlayer(connection);
                }
            }

            //If we have a player, update the ban information
            if (player != null)
            {
                //Update the results (if nessary)
                if (string.IsNullOrEmpty(ban.IP) || query.GetBool("copy_details", false)) ban.IP = player.IP;
                if (string.IsNullOrEmpty(ban.UUID) || query.GetBool("copy_details", false)) ban.UUID = player.UUID;
            }

            //Make sure we have a IP or UUID
            if (ban.BanType == BanType.Invalid)
                return new RestResponse(RestStatus.BadRequest, "Invalid ban type. IP and/or UUID must be set!");

            //Update the moderator if required
            if (string.IsNullOrEmpty(ban.Moderator))
                ban.Moderator = Authentication.Name;

            //Apply the correct date time
            ban.CreatedDate = DateTime.UtcNow;

            //Perform the ban and get the ban result
            try
            {
                var task = Starbound.Ban(ban);
                ban = Starbound.Configurator.GetBanAsync(task.Result).Result;
            }
            catch
            {
                return new RestResponse(RestStatus.InternalError, "Failed to add the ban, likely due to an invalid IP Address.");
            }
            
            //Kick the player if nessary, waiting for it to finish.
            if (player != null)
                Starbound.Kick(player.Connection, ban.Reason).Wait();
            
            //Return the ban
            return new RestResponse(RestStatus.OK, res: ban);
        }
        
    }
}
