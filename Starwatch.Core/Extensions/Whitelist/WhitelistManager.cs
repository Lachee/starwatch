using Starwatch.Entities;
using Starwatch.Monitoring;
using Starwatch.Starbound;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Starwatch.Extensions.Whitelist
{
    public class WhitelistManager : ConfigurableMonitor
    {
        private const string CONFIG_WORLDS_KEY = "worlds";
        public string KickFormat => Configuration.GetString("kick_format",
@"^orange;You have been kicked ^white;automatically ^orange;for trying to access a {mode} world.

^blue;If you would like access, please make a request at
^pink;https://iLoveBacons.com/requests/");


        [System.Obsolete]
        private Dictionary<string, ProtectedWorld> _protectedWorlds = new Dictionary<string, ProtectedWorld>();

        public WhitelistManager(Server server) : base(server, "Whitelist") { }

        public override Task Initialize()
        {
            //Sub to the teleport events
            Server.Connections.OnPlayerUpdate += OnPlayerUpdate;

            //Return done
            return Task.CompletedTask;
        }

        private async void OnPlayerUpdate(Player player)
        {
            if (player.Location == null) return;

            //Load the protected world
            ProtectedWorld pw = await GetProtectionAsync(player.Location);
            if (pw != null && await pw.CheckPermissionAsync(player))
            {
                Logger.Log("Player {0} is not allowed in world {1} because of {2}", player, pw.World, pw.Mode);
                await Server.Kick(player, KickFormat.Replace("{mode}", pw.Mode.ToString()));
            }
        }
        
        /// <summary>
        /// Gets a protection
        /// </summary>
        /// <param name="whereami"></param>
        /// <returns></returns>
        public async Task<ProtectedWorld> GetProtectionAsync(World world)
        {
            ProtectedWorld pw = new ProtectedWorld(this, world);
            if (!await pw.LoadAsync(Server.DbContext)) return null;
            return pw;
        }

        /// <summary>
        /// Sets or Creates a world protection
        /// </summary>
        /// <param name="whereami"></param>
        /// <param name="mode"></param>
        /// <param name="allowAnonymous"></param>
        /// <returns></returns>
        public async Task<ProtectedWorld> SetProtectionAsync(World world, WhitelistMode mode, bool allowAnonymous)
        {
            var protection = await GetProtectionAsync(world);
            if (protection == null) protection = new ProtectedWorld(this, world);

            protection.Mode = mode;
            protection.AllowAnonymous = allowAnonymous;
            await protection.SaveAsync(DbContext);

            return protection;
        }
        
        /// <summary>
        /// Removes a world protection
        /// </summary>
        /// <param name="world"></param>
        /// <returns></returns>
        public async Task<bool> RemoveProtectionAsync(ProtectedWorld world)
        {
            return await world.DeleteAsync();
        }
        
        
    }
}
