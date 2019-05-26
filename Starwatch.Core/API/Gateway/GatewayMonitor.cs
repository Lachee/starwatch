using Starwatch.Entities;
using Starwatch.Starbound;
using System.Threading.Tasks;
using Starwatch.API.Rest.Route;

namespace Starwatch.API.Gateway
{
    internal class GatewayMonitor : Monitoring.Monitor
    {
        public ApiHandler Api => Server.Starwatch.ApiHandler;
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
            Server.Settings.Accounts.OnAccountAdd += (account) =>
                Api.BroadcastRoute(new AccountDetailsRoute(account), "OnAccountAdd");

            Server.Settings.Accounts.OnAccountUpdate += (account) =>
                Api.BroadcastRoute(new AccountDetailsRoute(account), "OnAccountUpdate");

            Server.Settings.Accounts.OnAccountRemove += (name) =>
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
