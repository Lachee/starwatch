using Starwatch.Entities;
using Starwatch.Starbound;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Diagnostics;
using Starwatch.API.Rest.Route;

namespace Starwatch.API.Gateway
{
    internal class GatewayMonitor : Monitoring.Monitor
    {
        public ApiHandler Api => Server.Starwatch.ApiHandler;
        public GatewayMonitor(Server server) : base(server, "Gateway")
        {
            server.Connections.OnPlayerConnect += (player) =>
                Api.BroadcastRoute(new PlayerDetailsRoute(player), "OnPlayerConnect");
            
            server.Connections.OnPlayerDisconnect += (player, reason) =>
                Api.BroadcastRoute(new PlayerDetailsRoute(player), "OnPlayerDisconnect", reason: reason);

            server.Connections.OnPlayerUpdate += (player) =>
                Api.BroadcastRoute(new PlayerDetailsRoute(player), "OnPlayerUpdate");
            
            server.OnRconClientCreated += (starboundServer, rconClient) =>
                rconClient.OnServerReload += (sender, rconResponse) =>
                    Api.BroadcastRoute(new ServerRoute(), "OnServerReload");
            
        }

        public override Task OnServerStart()
        {
            Api.BroadcastRoute(new ServerRoute(), "OnServerStart");
            return Task.CompletedTask;
        }

        public override Task OnServerExit()
        {
            Api.BroadcastRoute(new ServerRoute(), "OnServerExit");
            return Task.CompletedTask;
        }

        public override async Task<bool> HandleMessage(Message msg) { await SendLogEvent(msg); return false; }

        /// <summary>
        /// Sends the log event to every gateway connection that is listening
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private async Task SendLogEvent(Message message)
        {
            //foreach (var gateway in Api.GetGatewayConnections<GatewayJsonConnection>("/events").Where(gw => !gw.HasTerminated))
            //    gateway.SendEvent("LOG", message);

            foreach (var gateway in Api.GetGatewayConnections<GatewayLogConnection>("/log").Where(gw => gw.Filter.LogEvents && gw.IsReady))
            {
                await gateway.SendPayload(new Payload.LogEvent() { Message = message });
            }
        }        
    }
}
