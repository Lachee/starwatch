using Starwatch.Entities;
using Starwatch.Exceptions;
using Starwatch.Starbound;
using Starwatch.Util;
using System;
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
                    var coloni = msg.Content.IndexOf(':');
                    var client = msg.Content.Cut(DISAGREEMENT_KEY.Length + 1, coloni);
                    var report = new DisagreementCrashReport() { Content = msg.ToString() };

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

                        //ban the players
                        report.Player = player;
                        await Server.Ban(player, BanFormat, "disagrement-monitor", false, false);
                    }
                    else
                    {
                        Logger.LogWarning("Failed to parse the client " + client + ", giving up and just aborting.");
                    }

                    //Send a API log to the error
                    Server.ApiHandler.BroadcastRoute((gateway) =>
                    {
                        if (gateway.Authentication.AuthLevel < API.AuthLevel.Admin) return null;
                        return report;
                    }, "OnDisagreementCrash");

                    //Throw the exception, forcing shutdown
                    throw new ServerShutdownException("Disagreement Exception");
                }
            }
            catch (Exception e)
            {
                //Throw an error and just abort by default.
                Logger.LogError(e);
                Server.LastShutdownReason = $"Disagreement Shutdown: {e.Message}";
                return true;
            }

            return false;
        }

        struct DisagreementCrashReport
        {
            public string Content { get; set; }
            public Player Player { get; set; }
        }
    }
}
