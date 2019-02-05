using Starwatch.Entities;
using Starwatch.Starbound;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Diagnostics;

namespace Starwatch.API.Gateway
{
    internal class GatewayMonitor : Monitoring.Monitor
    {
        public ApiHandler Api => Server.Starwatch.ApiHandler;
        public GatewayMonitor(Server server) : base(server, "Gateway")
        {
            server.Connections.OnPlayerConnect += async (p) => await SendPlayerEvent(p, Payload.PlayerEvent.EVENT_CONNECT);
            server.Connections.OnPlayerDisconnect += async (p, r) => await SendPlayerEvent(p, Payload.PlayerEvent.EVENT_DISCONNECT, r);
            server.Connections.OnPlayerUpdate += async (p) => await SendPlayerEvent(p, Payload.PlayerEvent.EVENT_UPDATE);
            server.OnRconClientCreated += (starboundServer, rconClient) => rconClient.OnServerReload += async (r, res) => await SendServerEvent(Payload.ServerEvent.EVENT_RELOAD);
            
        }

        public override async Task OnServerStart() { await SendServerEvent(Payload.ServerEvent.EVENT_START); }
        public override async Task OnServerExit() { await SendServerEvent(Payload.ServerEvent.EVENT_EXIT); }
        public override async Task<bool> HandleMessage(Message msg) { await SendLogEvent(msg); return false; }

        /// <summary>
        /// Sends the log event to every gateway connection that is listening
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private async Task SendLogEvent(Message message)
        {
            foreach (var gateway in Api.GetGatewayConnections().Where(gw => gw.Filter.LogEvents && gw.IsReady))
            {
                await gateway.SendPayload(new Payload.LogEvent() { Message = message });
            }
        }

        /// <summary>
        /// Sends the player event to every gateway connection that is listening
        /// </summary>
        /// <param name="player"></param>
        /// <param name="evt"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        private async Task SendPlayerEvent(Player player, string evt, string reason = "")
        {
            foreach (var gateway in Api.GetGatewayConnections().Where(gw => gw.Filter.PlayerEvents && gw.IsReady))
            {
                try
                {
                    await gateway.SendPayload(new Payload.PlayerEvent() { Event = evt, Player = player });
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "Exception occured while processing p." + evt + " Gateway " + gateway.Identifier + ": {0}");
                }
            }
        }

        /// <summary>
        /// Sends the server event to every gateway connection that is listening.
        /// </summary>
        /// <param name="evt"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        private async Task SendServerEvent(string evt, string reason = "")
        {
            foreach (var gateway in Api.GetGatewayConnections().Where(gw => gw.Filter.ServerEvents && gw.IsReady))
            {
                try
                {
                    await gateway.SendPayload(new Payload.ServerEvent() { Event = evt, Reason = reason });
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "Exception occured while processing s."+evt+" Gateway " + gateway.Identifier + ": {0}");
                }
            }
        }
    }
}
