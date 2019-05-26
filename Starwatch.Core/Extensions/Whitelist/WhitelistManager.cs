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

        private Dictionary<string, ProtectedWorld> _protectedWorlds = new Dictionary<string, ProtectedWorld>();
        public WhitelistManager(Server server) : base(server, "Whitelist")
        {
        }

        public override Task Initialize()
        {
            //Load the config
            _protectedWorlds = Configuration.GetObject(CONFIG_WORLDS_KEY, new Dictionary<string, ProtectedWorld>());

            //Sub to the teleport events
            Server.Connections.OnPlayerUpdate += async (player) =>
            {
                if (player.Location != null)
                {
                    ProtectedWorld world;
                    if (_protectedWorlds.TryGetValue(player.Location.Whereami, out world))
                    {
                        if (!world.ValidateAccount(player.AccountName))
                        {
                            Logger.Log("Player " + player + " is not allowed in world " + world.World + " because of " + world.Mode);
                            await Server.Kick(player, KickFormat.Replace("{mode}", world.Mode.ToString()));
                        }
                    }

                }
            };

            //Return done
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// Adds a world to be protected
        /// </summary>
        /// <param name="world"></param>
        /// <param name="mode"></param>
        /// <param name="allowAnonymous"></param>
        /// <returns></returns>
        public bool AddWorld(World world, WhitelistMode mode, bool allowAnonymous = false, string name = null)
        {
            if (_protectedWorlds.ContainsKey(world.Whereami)) return false;
            _protectedWorlds.Add(world.Whereami, new ProtectedWorld(world, mode, allowAnonymous, name));
            Configuration.SetKey(CONFIG_WORLDS_KEY, _protectedWorlds, save: true);
            return true;
        }

        /// <summary>
        /// Removes a world from protection
        /// </summary>
        /// <param name="world"></param>
        /// <returns></returns>
        public bool RemoveWorld(World world)
        {
            bool success = _protectedWorlds.Remove(world.Whereami);
            Configuration.SetKey(CONFIG_WORLDS_KEY, _protectedWorlds, save: success);
            return success;
        }

        /// <summary>
        /// Gets the worlds protection. Returns null if it does not exist.
        /// </summary>
        /// <param name="world"></param>
        /// <returns></returns>
        public ProtectedWorld GetWorld(World world)
        {
            ProtectedWorld value;
            if (_protectedWorlds.TryGetValue(world.Whereami, out value)) return value;
            return null;
        }

        public IEnumerable<ProtectedWorld> GetWorlds() => _protectedWorlds.Values;
        public IEnumerable<ProtectedWorld> GetWorlds(Account account) => _protectedWorlds.Values.Where(w => w.HasAccount(account.Name));

        /// <summary>
        /// Adds an account to the protected world.
        /// </summary>
        /// <param name="world"></param>
        /// <param name="account"></param>
        /// <returns></returns>
        public bool AddAccount(World world, Account account) => AddAccount(GetWorld(world), account);
        public bool AddAccount(ProtectedWorld world, Account account) 
        {

            if (world == null || account == null) return false;
            if (world.AccountList.Add(account.Name))
            {
                Save();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Adds a list of accounts to the protected account. Returns the number of accounts added
        /// </summary>
        /// <param name="world"></param>
        /// <param name="account"></param>
        /// <returns></returns>
        public int AddAccounts(World world, HashSet<string> accounts) => AddAccounts(GetWorld(world), accounts);
        public int AddAccounts(ProtectedWorld world, HashSet<string> accounts)
        {
            if (world == null || accounts == null) return 0;

            //Add the accounts and tally the results
            int count = 0;
            foreach(var acc in accounts)
                if (world.AccountList.Add(acc)) count++;

            //The count is more than 0, so save it
            if (count > 0) Save();
            return count;
        }


        /// <summary>
        /// Removes an account from the protected world
        /// </summary>
        /// <param name="world"></param>
        /// <param name="account"></param>
        /// <returns></returns>  
        public bool RemoveAccount(World world, Account account) => RemoveAccount(GetWorld(world), account);
        public bool RemoveAccount(ProtectedWorld world, Account account)
        {
            if (world == null || account == null) return false;
            if (world.AccountList.Remove(account.Name))
            {
                Save();
                return true;
            }

            return false;
        }
        

        /// <summary>
        /// Saves the configuration settings
        /// </summary>
        public void Save()
        {
            Configuration.SetKey(CONFIG_WORLDS_KEY, _protectedWorlds, save: true);
        }
    }
}
