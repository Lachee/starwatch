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
using System;
using System.Threading.Tasks;
using Starwatch.Entities;
using Starwatch.Logging;
using Starwatch.Starbound;
using Starwatch.Util;

namespace Starwatch.Monitoring
{
    class VpnMonitor : ConfigurableMonitor
    {
        public string NoAddressReason { get; private set; } = "Malformed connection.";
        public string AnonymousVpnReason { get; private set; } = "Invalid connection.";
        public string IPv6Reason { get; private set; } = "IPv6 is not supported on this platform.";

        public bool AllowVPN { get; set; }
        public bool AllowAnonymousVPN { get; set; }

        public VpnMonitor(Server server) : base(server, "VPNMonitor")
        {
            server.Connections.OnPlayerConnect += OnPlayerConnected;
        }

        public override Task Initialize()
        {
            NoAddressReason     = Configuration.GetString("reason_noaddress", NoAddressReason);
            AnonymousVpnReason  = Configuration.GetString("reason_anonvpn", AnonymousVpnReason);
            IPv6Reason          = Configuration.GetString("reason_ipv6", IPv6Reason);
            AllowVPN            = Configuration.GetBool("allow_vpn", true);
            AllowAnonymousVPN   = Configuration.GetBool("allow_anonvpn", false);

            return Task.CompletedTask;
        }

        private async void OnPlayerConnected(Player player)
        {
            //Kick anyone that doesn't have an IP address due to a 'Time Out'
            if (string.IsNullOrWhiteSpace(player.IP))
            {
                Logger.Log("Kicking {0} for no address.", player);
                await Server.Kick(player, NoAddressReason);
                return;
            }

            //Kick anyone that is VPN but doesnt have an account.
            if (player.IsVPN && (!AllowVPN || (!AllowAnonymousVPN && player.IsAnonymous)))
            {
                Logger.Log("Kicking {0} for Anonymous VPN.", player);
                await Server.Kick(player, AnonymousVpnReason);
                return;
            }

            //Kick anyone that is IPv6 but doesnt have an account.
            if (VPNValidator.IsIPv6(player.IP) && player.IsAnonymous)
            {
                Logger.Log("Kicking {0} for Anonymous IPv6.", player);
                await Server.Kick(player, IPv6Reason);
                return;
            }
            
            //TODO: Check All Anonymous Connections
            //TODO: Implement DEFCON. We only want to check this on a high defcon. 10 levels of defcon maybe?
        }
    }
}
