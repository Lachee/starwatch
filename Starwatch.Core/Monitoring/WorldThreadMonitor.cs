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
        public string BanFormat => 
@"^orange;You have been banned ^white;automatically ^orange;for causing a world exception.
^red;{exception}

^orange;Your ^pink;ticket ^orange; is ^white;{ticket}

^blue;Please make an appeal at
^pink;https://iLoveBacons.com/request/";

        public string KickFormat =>
@"^orange;You have been kicked for causing a world exception.
^red;{exception}";

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
            if (msg.Level != Message.LogLevel.Error) return false;

            //Prepare the report
            DisagreementCrashReport report = null;

            try
            {
                if (msg.Content.StartsWith(ERROR_MESSAGE))
                {
                    //Attempt to find the person
                    var coloni = msg.Content.IndexOf(':');
                    var client = msg.Content.Cut(ERROR_MESSAGE.Length + 1, coloni);
                    var exception = msg.Content.Substring(coloni);
                    report = new DisagreementCrashReport() { Content = msg.ToString(), Exception = exception.Trim() };

                    //Locate the player just for the sake of reporting
                    if (int.TryParse(client, out var cid))
                        report.Player = Server.Connections.GetPlayer(cid);

                    //If we are an IO exception, throw.
                    if (report.Exception.Contains("(IOException)"))
                        throw new ServerShutdownException("World Thread Exception - IO Exception");

                    /* Don't do any of this. We want to leave the palyer as is. They can go free.
                    //Parse the name
                    if (int.TryParse(client, out var cid))
                    {
                        //Find the player
                        report.Player = Server.Connections.GetPlayer(cid);
                        if (report.Player == null)
                        {
                            Logger.LogWarning("Could not find the person we were holding accountable! Using last person instead.");
                            report.Player = Server.Connections.LastestPlayer;
                        }

                        //If we have a player, we need to determine best course of action
                        if (report.Player != null)
                        {
                            //World thread exception caught
                            if (report.Exception.Contains("(MapException) Key "))
                            {
                                //Ban the player
                                await report.Player.Ban(BanFormat.Replace("{exception}", report.Exception), "World Thread Monitor");

                                //Send the report
                                throw new ServerShutdownException("World Thread Exception - Key Crash");
                            }

                            if (report.Exception.Contains("(IOException)"))
                                throw new ServerShutdownException("World Thread Exception - IO Exception");

                            //Regular exception caught, just kick the player and return early so we don't process the other reboot logic
                            await report.Player.Kick(KickFormat.Replace("{kick}", report.Exception));
                            return false;
                        }
                    }
                    else
                    {
                        //We didn't parse anything?
                        Logger.LogWarning("Failed to parse the client " + client + ", giving up and just aborting.");
                    }

                    //We have no user crashing us, so we will throw a general exception to get the server to reboot
                    throw new ServerShutdownException("World Thread Exception - General Error");
                    */
                }
            }
            catch (Exception e)
            {
                //Our logic threw an exception, most likely a ServerShutdownException. Lets abort and restart the server.
                Logger.LogError(e);
                Server.LastShutdownReason = $"Disagreement Shutdown ({e.GetType().Name}): {e.Message}\n" + e.StackTrace;
                return true;
            }
            finally
            {
                //Broadcast the report if it isn't null
                if (report != null)
                    BroadcastReport(report);
            }

            //We don't want to restart.
            return false;
        }

        private void BroadcastReport(DisagreementCrashReport report)
        {
            if (report == null) return;

            //Abort and giveup
            //Send a API log to the error
            Server.ApiHandler.BroadcastRoute((gateway) =>
            {
                if (gateway.Authentication.AuthLevel < API.AuthLevel.Admin) return null;
                return report;
            }, "OnWorldThreadException");
        }

        class DisagreementCrashReport
        {
            public string Content { get; set; }
            public string Exception { get; set; }
            public Player Player { get; set; }
        }
    }
}
