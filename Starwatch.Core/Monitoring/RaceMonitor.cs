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
^pink;https://iLoveBacons.com/request/");

        private int _errorTally = 0;
        private int _errorConnection = 0;
        private string _errorKey = "";
        public override int Priority => 59;

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

                //We need to validate to make sure the key isn't the coordinate exploit
                if (newKey[0] == '(')
                {
                    Logger.LogError("Possible coordinate exploit: {0}", newKey);
                    return false;
                }

                //If the key is different, then reset our counters.
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

                    var account = await player.GetAccountAsync();
                    if (account != null && account.IsAdmin)
                    {
                        //If the player is a admin, then we will just kick them
                        // We will wait some time just for the kick to be applied.
                        await Server.Kick(player, reason);
                        await Task.Delay(100);
                    }
                    else
                    {
                        //We will ban the player because they are not an admin
                        await Server.Ban(player, reason, "race-monitor", false, false);
                    }

                    //Throw an exception, telling the server to shutdown
                    throw new Exceptions.ServerShutdownException("Custom race detected");
                }
            }            

            return false;
        }
    }
}
