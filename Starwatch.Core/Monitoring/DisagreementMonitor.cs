using Starwatch.Entities;
using Starwatch.Starbound;
using Starwatch.Util;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Starwatch.Monitoring
{
    class DisagreementMonitor : ConfigurableMonitor
    {
        public const string DISAGREEMENT_KEY = "WorldServerThread exception caught handling incoming packets for client";
        public string BanFormat => Configuration.GetString("ban_format",
@"^orange;You have been banned ^white;automatically ^orange;for using mods that are NOT multiplayer friendly.
^orange;Your ^pink;ticket ^orange; is ^white;{ticket}

^blue;Please make an appeal at
^pink;https://iLoveBacons.com/requests/");
        
        public DisagreementMonitor(Server server) : base(server, "RACE")
        {
        }

        public override Task Initialize()
        {
            Logger.Log("Disagrement Ban Message: " + BanFormat);
            return Task.CompletedTask;
        }

        public override async Task<bool> HandleMessage(Message msg)
        {
            if (msg.Level != Message.LogLevel.Error) return false;

            try
            {
                if (msg.Content.StartsWith(DISAGREEMENT_KEY))
                {
                    //Attempt to find the person
                    int colonIndex = msg.Content.IndexOf(':');
                    string client = msg.Content.Cut(DISAGREEMENT_KEY.Length + 1, colonIndex);

                    //Parse the name
                    if (int.TryParse(client, out var cid))
                    {
                        //Find the player
                        Player player = Server.Connections.GetPlayer(cid);
                        if (player == null)
                        {
                            Logger.LogWarning("Could not find the person we were holding accountable! Using last person instead.");
                            player = Server.Connections.LastestPlayer;
                        }

                        //ban the player
                        await Server.Ban(player, BanFormat, "disagrement-monitor", false, false);
                        return true;
                    }
                    else
                    {
                        Logger.LogWarning("Failed to parse the client " + client + ", giving up and just aborting.");
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                //Throw an error and just abort by default.
                Logger.LogError(e);
                return true;
            }

            return false;
        }

    }
}
