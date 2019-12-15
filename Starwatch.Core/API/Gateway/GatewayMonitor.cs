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
using System.Threading.Tasks;
using Starwatch.API.Rest.Route;

namespace Starwatch.API.Gateway
{
    internal class GatewayMonitor : Monitoring.Monitor
    {
        public ApiHandler Api => Server.Starwatch.ApiHandler;
        public override int Priority => 21;

        public GatewayMonitor(Server server) : base(server, "Gateway")
        {
            //=== Player EVENTS
            server.Connections.OnPlayerConnect += (player) =>
                Api.BroadcastRoute(new PlayerDetailsRoute(player), "OnPlayerConnect", query: new Web.Query() { { "skip_list", "true" } });

            server.Connections.OnPlayerDisconnect += (player, reason) =>
                Api.BroadcastRoute(new PlayerDetailsRoute(player), "OnPlayerDisconnect", reason: reason,  query: new Web.Query() { { "skip_list", "true" } });

            server.Connections.OnPlayerUpdate += (player) =>
                Api.BroadcastRoute(new PlayerDetailsRoute(player), "OnPlayerUpdate", query: new Web.Query() { { "skip_list", "true" } });


            //=== Server Events
            server.OnKick += (sender, connection, reason, duration) =>
                Api.BroadcastRoute(new PlayerDetailsRoute(server.Connections.GetPlayer(connection)), "OnKick", reason);

            server.OnBan += (sender, ban) =>
                Api.BroadcastRoute(new BanDetailsRoute(ban), "OnBan", ban.Reason);
            
            //=== Account Events

            //=== RCON EVENTS
            server.OnRconClientCreated += (starboundServer, rconClient) =>
            {
                rconClient.OnServerReload += (sender, rconResponse) =>
                    Api.BroadcastRoute(new ServerRoute(), "OnServerReload");

                rconClient.OnExecuteSuccess += (sender, rconResponse) =>
                    Api.BroadcastRoute((gateway) =>
                    {
                        if (!gateway.Authentication.IsBot) return null;
                        return rconResponse;
                    }, "OnRconExecuteSuccess");

                rconClient.OnExecuteFailure += (sender, rconResponse) =>
                     Api.BroadcastRoute((gateway) =>
                     {
                         if (!gateway.Authentication.IsBot) return null;
                         return rconResponse;
                     }, "OnRconExecuteFailure");
            };
        }

        public override Task OnServerPreStart()
        {
            Server.Configurator.OnAccountAdd += (account) =>
                Api.BroadcastRoute(new AccountDetailsRoute(account), "OnAccountAdd");

            Server.Configurator.OnAccountUpdate += (account) =>
                Api.BroadcastRoute(new AccountDetailsRoute(account), "OnAccountUpdate");

            Server.Configurator.OnAccountRemove += (name) =>
                Api.BroadcastRoute((gateway) => name, "OnAccountRemove");

            return base.OnServerPreStart();
        }


        public override Task OnServerStart()
        {
            Api.BroadcastRoute(new ServerRoute(), "OnServerStart");
            return Task.CompletedTask;
        }

        public override Task OnServerExit(string reason)
        {
            Api.BroadcastRoute(new ServerRoute(), "OnServerExit", reason: reason);
            return Task.CompletedTask;
        }

        public override Task<bool> HandleMessage(Message msg)
        {
            if (msg.IsChat)
            {
                //Chats can go over regular events
                Api.BroadcastRoute((gateway) =>
                {
                    if (!gateway.Authentication.IsAdmin) return null;
                    return msg;
                }, "OnChat");
            }

            //Broadcast the log
            Api.BroadcastLog(msg.ToString());
            return Task.FromResult(false);
        }      
    }
}
