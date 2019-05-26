using Starwatch.Entities;
using Starwatch.Starbound;
using Starwatch.Util;
using System.Threading.Tasks;

namespace Starwatch.Monitoring
{
    public class RaceMonitor : ConfigurableMonitor
    {
        public const string EXCEPTION_MAP_KEY = "UniverseServer: exception caught: (MapException)";

        public string BanFormat => Configuration.GetString("ban_format", 
@"^orange;You have been banned ^white;automatically ^orange;for using custom races.
We detected the race ^white;'{key}'
^orange;Your ^pink;ticket ^orange; is ^white;{ticket}

^blue;Please make an appeal at
^pink;https://iLoveBacons.com/requests/");

        private int _errorTally = 0;
        private int _errorConnection = 0;
        private string _errorKey = "";

        public RaceMonitor(Server server) : base(server, "RACE")
        {
        }

        public override Task Initialize()
        {
            Logger.Log("Ban Message: " + BanFormat);
            return Task.CompletedTask;
        }

        public override async Task<bool> HandleMessage(Message msg)
        {
            if (msg.Level != Message.LogLevel.Error) return false;


            if (msg.Content.StartsWith(EXCEPTION_MAP_KEY))
            {
                //Check its key
                string newKey = msg.Content.Cut(EXCEPTION_MAP_KEY.Length + 6, msg.Content.Length - 25);
                if (newKey != _errorKey)
                {
                    //Reset the tally and the connection
                    _errorKey = newKey;
                    _errorTally = 0;
                    _errorConnection = -1;
                }

                //If this is our first error, we need to hold the lastest player accountable
                if (_errorTally == 0)
                {
                    _errorConnection = Server.Connections.LatestConnectedID;
                    Logger.Log("Initial race crash detected. Holding {0} accountable.", _errorConnection);
                }

                //Its happened 3 times, so we need to ban that person (if not already)
                if (_errorTally++ >= 3)
                {
                    Logger.LogError("Race crash detected, banning the player automatically and restarting the server.");

                    //Make sure the player exists. If not than we will get hte previous player
                    Player player = Server.Connections.GetPlayer(_errorConnection);
                    if (player == null)
                    {
                        Logger.LogWarning("Could not find the person we were holding accountable! Using last person instead.");
                        player = Server.Connections.LastestPlayer;
                    }

                    //Send a API log to the error
                    Server.ApiHandler.BroadcastRoute((gateway) =>
                    {
                        if (gateway.Authentication.AuthLevel < API.AuthLevel.Admin) return null;
                        return msg;
                    }, "OnCustomRaceCrash");

                    //Prepare the reason
                    string reason = BanFormat.Replace("{key}", _errorKey);

                    //Ban and restart
                    await Server.Ban(player, reason, "race-monitor", false, false);
                    throw new Exceptions.ServerShutdownException("Custom race detected");
                }
            }            

            return false;
        }
    }
}
