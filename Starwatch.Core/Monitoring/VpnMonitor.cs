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
        public string NoAddressReason = "Invalid connection.";
        public string AnonymousVpnReason = "Invalid connection.";
        public string IPv6Reason = "IPv6 is not supported on this platform.";

        public bool AllowVPN { get; set; }
        public bool AllowAnonymousVPN { get; set; }

        public VpnMonitor(Server server) : base(server, "VPN")
        {
            server.Connections.OnPlayerConnect += OnPlayerConnected;
            NoAddressReason     = Configuration.GetString("reason_noaddress", NoAddressReason);
            AnonymousVpnReason  = Configuration.GetString("reason_anonvpn", AnonymousVpnReason);
            IPv6Reason          = Configuration.GetString("reason_ipv6", IPv6Reason);
            AllowVPN            = Configuration.GetBool("allow_vpn", true);
            AllowAnonymousVPN   = Configuration.GetBool("allow_anonvpn", false);
            Configuration.Save();
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
