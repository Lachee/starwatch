using Starwatch.Entities;
using Starwatch.Logging;
using Starwatch.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;

namespace Starwatch.Starbound
{

    /// <summary>
    /// Handles a list of connections
    /// </summary>
    public class Connections : Monitoring.Monitor
    {
        public static readonly bool ENFORCE_STRICT_NAMES = true;
        private static readonly Regex regexLoggedMsg = new Regex(@"'(.*)' as player '(.*)' from address (\d+\.\d+\.\d+\.\d+)", RegexOptions.Compiled);
        private static readonly Regex regexClientMsg = new Regex(@"'(.*)' <(\d+)> \((\d+\.\d+\.\d+\.\d+)\) (connected|disconnected)( for reason: (.*))?", RegexOptions.Compiled);

        /// <summary>
        /// The last ID to conenct
        /// </summary>
        public int LatestConnectedID { get; private set; }

        /// <summary>
        /// The last player to connect.
        /// </summary>
        public Player LastestPlayer => GetPlayer(LatestConnectedID);

        /// <summary>
        /// The count of connections on the server
        /// </summary>
        public int Count => _connections.Count;

        /// <summary>
        /// Gets a connection at the index, otherwise returns nunll.
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public Player this[int connection] => GetPlayer(connection);

        private Dictionary<int, Player> _connections = new Dictionary<int, Player>();
        private List<PendingPlayer> _pending = new List<PendingPlayer>();
        private struct PendingPlayer
        {
            public string account;
            public string character;
            public string address;
        }


        public delegate void OnPlayerUpdateEvent(Player player);
        public delegate void OnPlayerDisconnectEvent(Player player, string reason);

        public event OnPlayerUpdateEvent OnPlayerUpdate;
        public event OnPlayerUpdateEvent OnPlayerConnect;
        public event OnPlayerDisconnectEvent OnPlayerDisconnect;

        private Timer _uuidTimer;
        
        public Connections(Server server) : base (server, "CON")
        {
        }

        #region Events
        public override async Task<bool> HandleMessage(Message msg)
        {            
            //Only nicks are within chat messages so we should abort if it is.
            if (msg.Level == Message.LogLevel.Chat)
            {
                //Nickname has changed?
                if (msg.Content.StartsWith("/nick"))
                {
                    string nickname = msg.Content.Substring(6);
                    Player player = _connections.Values.Where(p => p.Username.Equals(msg.Author)).First();
                    if (player != null)
                    {
                        Logger.Log("Nickname set for " + player + " to " + nickname);
                        _connections[player.Connection].Nickname = nickname;
                        try
                        {
                            OnPlayerUpdate?.Invoke(_connections[player.Connection]);
                        }
                        catch (Exception e)
                        {
                            Logger.LogError(e, "OnPlayerUpdate Exception - Nickname: {0}");
                        }
                    }
                    else
                    {
                        Logger.LogError("Failed to set the nickname '{0}' on player '{1}' because they are not on the connection list!", nickname, msg.Author);
                    }
                }

                //Abort now since nicknames are the only chat evernts we care about
                return false;
            }

            //Only info logs contain useful information
            if (msg.Level != Message.LogLevel.Info) return false;

            //check if its a warp message
            if (msg.Content.StartsWith("UniverseServer: Warp") && !msg.Content.Contains("failed"))
            {
                int toIndex = msg.Content.IndexOf('t');
                string sub = msg.Content.Substring(31, toIndex - 1 - 31);
                int connection = int.Parse(sub);
                string location = msg.Content.Substring(toIndex + 3);
                
                if (_connections.ContainsKey(connection))
                {
                    Logger.Log("Player updated their location!");
                    _connections[connection].Location = World.Parse(location);
                    try
                    {
                        OnPlayerUpdate?.Invoke(_connections[connection]);
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e, "OnPlayerUpdate Exception: {0}");
                    }
                }
                else
                {
                    Logger.LogError("Attempted to warp a player that isn't in the connection list!");
                }

                //Abort now since only warp starts with UniverseServer: Warp
                return false;
            }

            //check if its a logged in message
            if (msg.Content.StartsWith("UniverseServer: Logged"))
            {
                //Match the regex
                var match = regexLoggedMsg.Match(msg.Content);
                if (match.Success)
                {
                    //Get the groups
                    string account = match.Groups[1].Value;
                    string character = match.Groups[2].Value;
                    string address = match.Groups[3].Value;

                    //Do some trimming of the accounts
                    if (account.Equals("<anonymous>")) account = null; else account = account.Substring(1, account.Length - 2);

                    //Add to the pending connections
                    _pending.Add(new PendingPlayer() {
                        address = address,
                        account = account,
                        character = character
                    });                    
                }
                else
                {
                    Logger.LogWarning("Unable to match logged message: {0}", msg.Content);
                }

                //Abort now since only logged starts with UniverseServer: Logged
                return false;
            }

            //check if the message is a connected message
            if (msg.Content.StartsWith("UniverseServer: Client"))
            {
                //Match the regex
                var match = regexClientMsg.Match(msg.Content);
                if (match.Success)
                {
                    //Get the data
                    string character = match.Groups[1].Value;
                    int connection = int.Parse(match.Groups[2].Value);
                    string address = match.Groups[3].Value;
                    bool wasConnection = match.Groups[4].Value == "connected";
                    string reason = match.Groups[6].Success ? match.Groups[6].Value : null;

                    if (wasConnection)
                    {
                        //If we are connecting, look for the matching pending connection
                        var pending = _pending.Where(pp => pp.address.Equals(address) && pp.character.Equals(character)).FirstOrDefault();
                        if (pending.character.Equals(character))
                        {
                            //Add them to the full list and remove them from pending
                            _pending.Remove(pending);
                            _connections.Add(connection, new Player(Server, connection)
                            {
                                AccountName = pending.account,
                                Username = pending.character,
                                IP = pending.address
                            });

                            //Send the event off
                            try
                            {
                                //Invoke the events. This shouldn't have to many listeners to this.
                                OnPlayerConnect?.Invoke(_connections[connection]);

                                //Make sure they have a valid username
                                await EnforceCharacterName(_connections[connection]);
                            }
                            catch (Exception e)
                            {
                                Logger.LogError(e, "OnPlayerConnect Exception: {0}", e);
                            }
                        }
                        else
                        {
                            Logger.LogError("Failed to find the pending request! " + address);
                        }
                    }
                    else
                    {
                        //It was a disconnection, so find them in the list of players
                        Player player;
                        if (_connections.TryGetValue(connection, out player))
                        {
                            try
                            {
                                //Invoke the events. This shouldn't have to many listeners to this.
                                OnPlayerDisconnect?.Invoke(player, reason ?? "Disconnected for unkown reasons");
                            }
                            catch (Exception e)
                            {
                                Logger.LogError(e, "OnPlayerDisconnect Exception: {0}", e);
                            }
                        }

                        //Remove the connection
                        _connections.Remove(connection);
                    }
                }
                else
                {
                    Logger.LogWarning("Unable to match client message: {0}", msg.Content);
                }

                //Abort now since only client starts with UniverseServer: Client
                return false;
            }

            return false;
        }

        public override Task OnServerStart()
        {
            if (Server.Rcon != null)
            {
                //Initialize the timer because its null
                if (_uuidTimer == null)
                {
                    _uuidTimer = new Timer(1 * 60 * 1000) { AutoReset = true };
                    _uuidTimer.Elapsed += async (sender, args) => await RefreshListing();
                }

                //Star tthe time
                _uuidTimer.Start();
            }

            return base.OnServerStart();
        }
        public override Task OnServerExit(string reason)
        {
            //Clear all pending connections and existing connections.
            _pending.Clear();
            _connections.Clear();

            //Stop the time if we have it.
            if (_uuidTimer != null) _uuidTimer.Stop();
            
            //We do not send any events as what ever client that listens to this should expect a server exit to result in 0 connections.
            return base.OnServerExit(reason);
        }
        #endregion

        #region Getters
        public IEnumerable<Player> GetPlayersEnumerator() => _connections.Values;
        public Player[] GetPlayers() => GetPlayersEnumerator().ToArray();
        public int[] GetConnectionIDs() =>_connections.Keys.ToArray();
        #endregion

        #region Helpers
        /// <summary>
        /// Gets the player at the given index
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Player GetPlayer(int id)
        {
            Player player;
            if (_connections.TryGetValue(id, out player)) return player;
            return null;
        }

        public async Task<bool> RefreshListing()
        {
            //Make sure listing is enabled
            if (Server.Rcon == null) return false;

            //Create a mapping of users and prepare some temporary array for additions and removals.
            var listedUsers = (await Server.Rcon.ListAsync()).ToDictionary(l => l.Connection);
            List<int> removals = new List<int>();

            //Go through each listing adding elements and updating existing elements
            foreach (var kp in _connections)
            {
                //Make sure its in the dictionary
                Rcon.StarboundRconClient.ListedPlayer listing;
                if (listedUsers.TryGetValue(kp.Key, out listing))
                {
                    //Update the UUID if nessary
                    if (kp.Value.UUID != listing.UUID)
                    {
                        Logger.Log("User " + kp.Value.Username + " updated their uuid");
                        kp.Value.UUID = listing.UUID;

                        try
                        {
                            //Invoke the events. This shouldn't have to many listeners to this.
                            OnPlayerUpdate?.Invoke(_connections[kp.Value.Connection]);
                        }
                        catch (Exception e)
                        {
                            Logger.LogError(e, "OnPlayerUpdate(timmed) Exception: {0}", e);
                        }
                    }

                    //Remove the value so we dont hit it again
                    listedUsers.Remove(kp.Key);
                }
                else
                {
                    //The user isnt in here so we will add them to our remove hashset.
                    Logger.Log("User " + kp.Value.Username + " disappeared");
                    removals.Add(kp.Key);
                }

                //Reset the timer (if it exists)
                _uuidTimer?.Reset();
            }

            //Remove all the old people that are left in the list
            foreach (int connection in removals)
            {
                //It was a disconnection, so find them in the list of players
                Player player;
                if (_connections.TryGetValue(connection, out player))
                {
                    try
                    {
                        //Invoke the events. This shouldn't have to many listeners to this.
                        OnPlayerDisconnect?.Invoke(player, "Not listed by server");
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e, "OnPlayerDisconnect(timmed) Exception: {0}", e);
                    }
                }

                _connections.Remove(connection);
            }

            //Add all the new people that are left in the list
            foreach (var kp in listedUsers)
            {
                //The user does not exist so we will add them
                Logger.Log("User " + kp.Value.Name + " ( " + kp.Value.Connection + " ) joined without us noticing.");

                //Add the new user
                _connections.Add(kp.Value.Connection, new Player(Server, kp.Value.Connection)
                {
                    Username = kp.Value.Name,
                    UUID = kp.Value.UUID,
                    IP = null
                });

                try
                {
                    //Invoke the events. This shouldn't have to many listeners to this.
                    OnPlayerConnect?.Invoke(_connections[kp.Value.Connection]);

                    //Make sure they have a valid username
                    await EnforceCharacterName(_connections[kp.Value.Connection]);
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "OnPlayerConnect(timmed) Exception: {0}", e);
                }
            }

            return true;
        }

        public async Task<World> RefreshLocation(Player player) => await RefreshLocation(player.Connection);
        public async Task<World> RefreshLocation(int connection)
        {
            //Make sure listing is enabled and make sure the player exists
            if (Server.Rcon == null) return null;
            if (!_connections.ContainsKey(connection))
            {
                Logger.LogError("Attempted to refresh the location of a null connection: {0}", connection);
                return null;
            }
            
            //Perform a whereis
            var response = await Server.Rcon.WhereisAsync(connection);
            if (response.Success)
            {
                //Update the location and execute the event
                Logger.Log("Player updated their location via whereis!");
                _connections[connection].Location = World.Parse(response.Message);
                try
                {
                    OnPlayerUpdate?.Invoke(_connections[connection]);
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "OnPlayerUpdate Exception: {0}");
                }

                //Return the new world
                return _connections[connection].Location;
            }
            else
            {
                Logger.LogWarning("Failed to refresh a players location: " + response.Message);
                return null;
            }
        }

        private async Task EnforceCharacterName(Player player)
        {
            if (!ENFORCE_STRICT_NAMES) return;
            if (player == null)
            {
                Logger.LogWarning("Trying to enforce the name of a null player!");
                return;
            }

            bool containsIllegalCharacters = false;
            if (player.Username.Contains('>')) containsIllegalCharacters = true;
            if (player.Username.Contains('<')) containsIllegalCharacters = true;
            if (player.Username.Contains(')')) containsIllegalCharacters = true;
            if (player.Username.Contains('(')) containsIllegalCharacters = true;
            if (player.Username.Contains("Lachee") && player.AccountName.ToLowerInvariant() != "lachee") containsIllegalCharacters = true;
            if (containsIllegalCharacters)
            {
                await Server.Kick(player.Connection, "Character name contains illegal characters.");
            }

            //Make sure no duplicate names
            if (_connections.Where(c => c.Value.Username.Equals(player.Username) && c.Value.Connection != player.Connection).Any())
            {
                await Server.Kick(player.Connection, "Character with the same name already exists on the server.");
            }
        }
        #endregion
    }
}

#if DUEOIFHBEO

[06:24:39.916] [Info] UniverseServer: Connection received from: 104.220.6.121:55214
[06:24:40.981] [Info] UniverseServer: Logged in account ''Kirisis'' as player '^cornflowerblue;Bane^reset;' from address 104.220.6.121
[06:24:40.998] [Info] UniverseServer: Client '^cornflowerblue;Bane^reset;' <8> (104.220.6.121) connected

#endif