using System.Threading.Tasks;
using Starwatch.Entities;
using Starwatch.Exceptions;
using Starwatch.Logging;
using Starwatch.Starbound;

namespace Starwatch.Monitoring
{
    class InvalidCoordMonitor : ConfigurableMonitor
    {
        //Flying ship for player 15 to 4327:3735:1393
        //UniverseServer: Creating new system world at location (4327, 3735, 1393)
        //UniverseServer: exception caught: (CelestialException) CelestialMasterDatabase::childOrbits called on invalid coordinate

        const string FLYING_SHIP = "Flying ship for player ";
        const string FATAL_ERROR = "UniverseServer: exception caught: (CelestialException) CelestialMasterDatabase::childOrbits called on invalid coordinate";

        public override int Priority => 51;

        /// <summary>
        /// The message to ban with
        /// </summary>
        public string BanReason => Configuration.GetString("ban_format",
@"^orange;You have been banned ^white;automatically ^orange;for generate invalid coordinates.
^orange;Your ^pink;ticket ^orange; is ^white;{ticket}

^blue;Please make an appeal at
^pink;https://iLoveBacons.com/request/");

        /// <summary>
        /// The detection we have found so far
        /// </summary>
        public InvalidCoordinateDetection Detection { get; private set; }
        public class InvalidCoordinateDetection
        {
            public Player Player { get; internal set; }
            public string Coordinate { get; internal set; }
        }

        public InvalidCoordMonitor(Server server) : base(server, "InvalidCoord")
        {
            Detection = new InvalidCoordinateDetection();
        }
        
		public override async Task<bool> HandleMessage(Message msg)
		{
            //Check if its a flying message
            if (msg.Level == Message.LogLevel.Info)
            {
                if (msg.Content.StartsWith(FLYING_SHIP))
                {
                    string substr = msg.Content.Substring(FLYING_SHIP.Length);
                    string[] parts = substr.Split(' ');

                    if (int.TryParse(parts[0], out var conid))
                    {
                        if (parts.Length >= 3 && parts[2].Contains(":"))
                        {
                            Detection.Player = Server.Connections.GetPlayer(conid);
                            Detection.Coordinate = parts[2];
                        }
                    }
                }
            }

            //We only care for error messages, which we will use to find segfaults.
            if (msg.Level == Message.LogLevel.Error)
            {
                if (msg.Content.Equals(FATAL_ERROR))
                {
                    Logger.LogError("A invalid coordinate has been generated. The last flying coordinate was {0} by player {1}", Detection.Coordinate, Detection.Player);

                    //Ban the player
                    if (Detection.Player != null && Detection.Player.IsConnected())
                    {
                        //Ban the player
                        await Server.Ban(Detection.Player, BanReason, "invalidcoord-monitor", true, true);

                        //Send a API log to the error
                        Server.ApiHandler.BroadcastRoute((gateway) =>
                        {
                            if (gateway.Authentication.AuthLevel < API.AuthLevel.Admin) return null;
                            return Detection;
                        }, "OnInvalidCoordinate");
                    }

                    //We have processed it, so dump the player.
                    Detection.Player = null;
                }
            }

            return false;
		}

	}
}
