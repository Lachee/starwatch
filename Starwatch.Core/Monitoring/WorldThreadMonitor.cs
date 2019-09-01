using Starwatch.Entities;
using Starwatch.Exceptions;
using Starwatch.Starbound;
using Starwatch.Util;
using System;
using System.Threading.Tasks;

namespace Starwatch.Monitoring
{
    class WorldThreadMonitor : ConfigurableMonitor
    {
        public const string ERROR_MESSAGE = "WorldServerThread exception caught handling incoming packets for client";
        public string BanFormat => Configuration.GetString("ban_format",
@"^orange;You have been banned ^white;automatically ^orange;for causing a world exception.
^red;{exception}

^orange;Your ^pink;ticket ^orange; is ^white;{ticket}

^blue;Please make an appeal at
^pink;https://iLoveBacons.com/request/");


        public override int Priority => 50;

        public WorldThreadMonitor(Server server) : base(server, "WORLD")
        {
        }

        public override Task Initialize()
        {
            Logger.Log("Disagrement Ban Message: " + BanFormat);
            return Task.CompletedTask;
        }

        public override async Task<bool> HandleMessage(Message msg)
        {
            //if (msg.Level != Message.LogLevel.Error) return false;

            try
            {
                if (msg.Content.StartsWith(ERROR_MESSAGE))
                {
                    //Attempt to find the person
                    var coloni = msg.Content.IndexOf(':');
                    var client = msg.Content.Cut(ERROR_MESSAGE.Length + 1, coloni);
                    var exception = msg.Content.Substring(coloni);
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

                        //Make sure we actually have a player before continuing the ban spree.
                        if (player != null)
                        {
                            //ban the players
                            report.Player = player;
                            await Server.Ban(player, BanFormat.Replace("{exception}", exception), "WorldThreadMonitor", false, false);
                        }
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
                    }, "OnWorldThreadCrash");

                    //Throw the exception, forcing shutdown
                    throw new ServerShutdownException("World Thread Exception");
                }
            }
            catch (Exception e)
            {
                //Throw an error and just abort by default.
                Logger.LogError(e);
                Server.LastShutdownReason = $"Disagreement Shutdown ("+e.GetType().Name+"): {e.Message}\n" + e.StackTrace;
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
