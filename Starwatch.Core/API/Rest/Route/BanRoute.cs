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

        /// <summary>
        /// Gets the last ban
        /// </summary>
        public override RestResponse OnGet(Query query)
        {
            return new RestResponse(RestStatus.OK, res: Starbound.Settings.CurrentBanTicket);
        }

        /// <summary>
        /// Creates a new ban
        /// </summary>
        public override RestResponse OnPost(Query query, object payloadObject)
        {
            //Prepare some values
            Ban ban = (Ban)payloadObject;
            Player player = null;

            //Validate the ban exists
            if (ban.Ticket.HasValue)
                return new RestResponse(RestStatus.BadRequest, msg: "Bans cannot have their tickets preset!");

            //If we have a connection ID then we will fill the rest with the connection information.
            // otherwise we will just ban the player as usual.
            if (query.ContainsKey("cid"))
            {
                //We are banning a specific connection id.
                int connection = 0;
                if (!query.TryGetInt("cid", out connection))
                    return new RestResponse(RestStatus.BadRequest, msg: $"Cannot convert '{query["cid"]}' to a int32!");

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

            //Perform the ban and get the ban result
            var task = Starbound.Ban(ban);
            ban = Starbound.Settings.GetBan(task.Result);
            
            //Kick the player if nessary, waiting for it to finish.
            if (player != null)
                Starbound.Kick(player.Connection, ban.Reason).Wait();
            
            //Return the ban
            return new RestResponse(RestStatus.OK, res: ban);
        }
        
    }
}
