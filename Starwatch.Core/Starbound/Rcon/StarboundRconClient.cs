using Starwatch.Entities;
using Starwatch.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Starwatch.Starbound.Rcon
{
    public class StarboundRconClient : SourceRconClient
    {
        public event RconEventHandler OnServerReload;
        public event RconEventHandler OnWhereis;
        public event RconEventHandler OnKick;
        public event RconEventHandler OnBan;
        public event RconEventHandler OnBroadcast;
        
        public Server Server { get; }
        public Logger Logger { get; }

        public StarboundRconClient(Server server) : base("127.0.0.1", server.Settings.RconServerPort, server.Settings.RconServerPassword)
        {
            Server = server;
            Logger = new Logger("RCON", server.Logger);

            Address = server.Settings.RconServerBind.Trim();
            if (string.IsNullOrWhiteSpace(Address) || Address.Equals("*") || Address.Equals("localhost")) Address = "127.0.0.1";

        }

        /// <summary>
        /// Reloads the server configuration
        /// </summary>
        /// <returns></returns>
        public async Task<RconResponse> ReloadServerAsync()
        {
            var response = await ExecuteAsync("serverreload");
            OnServerReload?.Invoke(this, response);
            return response;
        }

        /// <summary>
        /// Gets the current location of the connection.
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public async Task<RconResponse> WhereisAsync(int connection)
        {
            //"Client $2's current location is ClientShipWorld:b37ac1ec7e4e7c1874c78a7433737943"
            var response = await ExecuteAsync($"whereis ${connection}");
            if (response.Message.StartsWith("Client $"))
            {
                int length = $"Client ${connection}'s current location is ".Length;
                response.Message = response.Message.Substring(length);
                response.Success = true;
            }
            else
            {
                response.Success = false;
            }

            OnWhereis?.Invoke(this, response);
            return response;
        }

        /// <summary>
        /// Kicks a connection from the server. 
        /// </summary>
        /// <param name="connection">The connection to kick</param>
        /// <param name="reason">The reason to kick the connection.</param>
        /// <returns></returns>
        public async Task<RconResponse> Kick(int connection, string reason)
        {
            var response = await ExecuteAsync($"kick ${connection} \"{reason}\"");
            if (!(response.Success && response.Message.StartsWith("Successfully"))) response.Success = false;
            OnKick?.Invoke(this, response);
            return response;
        }

        /// <summary>
        /// Bans the identifier
        /// </summary>
        /// <param name="identifier">The starbound identifier of the player to ban</param>
        /// <param name="reason">The reason for the ban</param>
        /// <param name="type">The type of the ban</param>
        /// <param name="duration">The duration of the ban</param>
        /// <returns>Rcon Response of hte ban</returns>
        public async Task<RconResponse> Ban(int connection, string reason, BanType type, int duration)
        {
            if (type == BanType.Invalid)
            {
                return new RconResponse()
                {
                    Message = "Invalid ban type",
                    Success = false
                };
            }

            string mode = type == BanType.Complete ? "both" : (type == BanType.IP ? "ip" : "uuid");
            var response = await ExecuteAsync($"ban ${connection} \"{reason}\" {mode} {duration}");
            OnBan?.Invoke(this, response);
            return response;
        }

        /// <summary>
        /// Broadcasts a message
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<RconResponse> BroadcastAsync(string message)
        {
            var response = await ExecuteAsync($"say {message}");
            OnBroadcast?.Invoke(this, response);
            return response;
        }

        /// <summary>
        /// Lists an array of players currently on the server.
        /// </summary>
        /// <returns></returns>
        public async Task<ListedPlayer[]> ListAsync()
        {
            //Get a list of players
            var rcon = await ExecuteAsync("list");
            if (!rcon.Success)
            {
                //Failed to execute
                return new ListedPlayer[0];
            }

            //No accounts listed
            if (!rcon.Message.Contains(":")) return new ListedPlayer[0];

            //Split it from newlines. Lazy hack to get regex working
            string[] lines = rcon.Message.Split('\n');
            ListedPlayer[] players = new ListedPlayer[lines.Length];

            for (int i = 0; i < lines.Length; i++)
            {
                int firstBreak = lines[i].IndexOf(':'); //Variable index sizes means this is uncertain
                if (firstBreak < 0)
                {
                    //There is no valid information on this line
                    continue;
                }

                int lastBreak = lines[i].Length - 35;   //Working from the end, we know the UUID will always be 32, so we can calculate where it will be

                //Create the player object by splitting up the components. Regex is for cowards.
                players[i] = new ListedPlayer()
                {
                    Name = lines[i].Substring(firstBreak + 2, lastBreak - firstBreak - 4),
                    UUID = lines[i].Substring(lastBreak + 3),
                    Connection = int.Parse(lines[i].Substring(1, firstBreak - 2))
                };
            }

            //Parse the players
            return players;
        }

        /// <summary>
        /// Executes a RCON command and listens for its response. Will fail safely with a invalid rcon response being returned if any errors occured.
        /// <para>NOTE: Its recommended to always use the implementation over manually calling rcon. For example, call <see cref="ListAsync"/> to get a list of players.</para>
        /// </summary>
        /// <param name="command">The rcon command to execute</param>
        /// <returns>Returns a rcon resposne.</returns>
        public override async Task<RconResponse> ExecuteAsync(string command)
        {
            Logger.Log("Executing Command: " + command);
            return await base.ExecuteAsync(command);
        }

        /// <summary>
        /// A player that is listed in the list command
        /// </summary>
        public struct ListedPlayer
        {
            /// <summary>
            /// The name of the player
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// The connection id of the player
            /// </summary>
            public int Connection { get; set; }

            /// <summary>
            /// The uuid of the player
            /// </summary>
            public string UUID { get; set; }
        }
    }
}
