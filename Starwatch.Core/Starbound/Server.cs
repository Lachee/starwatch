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
using Newtonsoft.Json;
using Starwatch.Database;
using Starwatch.Entities;
using Starwatch.Logging;
using Starwatch.Monitoring;
using Starwatch.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

namespace Starwatch.Starbound
{

    /*
        TODO: Automatated backups of worlds time based.
        TODO: Way to restore the files manually. (ie: restore 10 minutes ago)
        TODO: List Backups
        TODO: World Restores automatically.
    */
    public class Server : IDisposable
    {
        public int Id => 1;

        public StarboundHandler Starwatch { get; private set; }
        public DbContext DbContext => Starwatch.DbContext;
        public Logger Logger { get; private set; }

        public string SamplePath { get; set; }
        public int SampleMinDelay { get; set; } = 250;
        public int SampleMaxDelay { get; set; } = 250 * 10;

        public string StorageDirectory => Path.Combine(WorkingDirectory, "..", "storage");
        public string WorkingDirectory { get; }
        public string ExecutableName { get; }
        public string ConfigurationFile { get; }

        /// <summary>
        /// Handles the state tracking of players connected to the server
        /// </summary>
        public Connections Connections { get; private set; }

        /// <summary>
        /// The configuration factory for the server
        /// </summary>
        public Configurator Configurator { get; private set; }
        
        /// <summary>
        /// The configuration for the server
        /// </summary>
        public Configuration Configuration { get; private set; }

        /// <summary>
        /// The RCON Client for the server.
        /// </summary>
        public Rcon.StarboundRconClient Rcon { get; private set; }

        /// <summary>
        /// The last message for the shutdown.
        /// </summary>
        public string LastShutdownReason { get; set; }

        public API.ApiHandler ApiHandler => Starwatch.ApiHandler;

        #region Events
        /// <summary>
        /// Called when the Rcon Client is created. Used for registering to Rcon events.
        /// </summary>
        public event ServerRconClientEventHandler OnRconClientCreated;
        public delegate void ServerRconClientEventHandler(object sender, Rcon.StarboundRconClient client);
        #endregion

        /// <summary>
        /// The time the server last started.
        /// </summary>
        public DateTime StartTime { get; private set; }

        /// <summary>
        /// The time the server last terminated.
        /// </summary>
        public DateTime EndTime { get; private set; }

        /// <summary>
        /// Is the server currently running?
        /// </summary>
        public bool IsRunning => StartTime > EndTime;

        private bool _terminate = false;

        private SBProcess _starbound;

        private List<Monitor> _monitors;

        public Server(StarboundHandler starwatch, Configuration configuration)
        {
            Configuration = configuration;
            WorkingDirectory = Configuration.GetString("directory", @"S:\Steam\steamapps\common\Starbound\win64");
            ExecutableName = Configuration.GetString("executable", @"starbound_server.exe");
            ConfigurationFile = Path.Combine(WorkingDirectory, "../storage/starbound_server.config");

            SamplePath = Configuration.GetString("sample_path", "");
            SampleMinDelay = configuration.GetInt("sample_min_delay", SampleMinDelay);
            SampleMaxDelay = configuration.GetInt("sample_max_delay", SampleMaxDelay);

            Starwatch = starwatch;
            Logger = new Logger("SERV", starwatch.Logger);

            Configurator = new Configurator(this, this.Logger.Child("CNF"));
        }

        /// <summary>
        /// Loads all the monitors from the config. This must be called for anything useful to happen
        /// </summary>
        /// <returns></returns>
        public async Task LoadAssemblyMonitorsAsync(Assembly assembly)
        {
            //Unload all previous monitors
            await UnloadMonitors();

            //Load up the connection
            Connections = new Connections(this);
            var monitors = new List<Monitor>();

            var types = assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(Monitor))).Where(t => !t.IsAbstract);
            foreach (var type in types)
            {
                var constructor = type.GetConstructor(typeof(Server));
                var instance = (Monitoring.Monitor) constructor.Invoke(this);
                monitors.Add(instance);

                if (type == typeof(Connections))
                    Connections = instance as Connections;
            }

            _monitors = new List<Monitor>();
            foreach(var m in monitors.OrderBy(m => m.Priority))
            {
                try
                {
                    _monitors.Add(m);
                    await m.Initialize();
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "Failed to load Monitor " + m.Name + ": {0}");
                }
            }
            

            //Just making sure things are okay.
            if (_monitors.Count <= 1) Logger.LogWarning("NOT MANY MONITORS REGISTERED! Did edit the config yet?");
        }

        /// <summary>
        /// Unloads all monitors and then disposes them.
        /// </summary>
        /// <returns></returns>
        public async Task UnloadMonitors()
        {
            if (_monitors == null) return;
            foreach (var m in _monitors)
            {
                await m.Deinitialize();
                m.Dispose();
            }

            _monitors.Clear();
        }

        public Monitor[] GetMonitors() => _monitors.ToArray();
        public Monitor[] GetMonitors(Type type) => _monitors.Where(m => m.GetType().IsAssignableFrom(type)).ToArray();
        public Monitor[] GetMonitors(string fullname) => _monitors.Where(m => m.GetType().FullName.Equals(fullname)).ToArray();
        public T[] GetMonitors<T>() where T : Monitor => _monitors.Where(m => m.GetType().IsAssignableFrom(typeof(T))).Select(m => (T)m).ToArray();

        /// <summary>
        /// Tries to get the first instance of a monitor.
        /// </summary>
        /// <param name="server"></param>
        /// <param name="monitor"></param>
        /// <returns></returns>
        public bool TryGetMonitor<T>(out T monitor) where T : Monitor
        {
            var monitors = GetMonitors<T>();
            if (monitors.Length == 0)
            {
                monitor = null;
                return false;
            }

            monitor = monitors[0];
            return true;
        }
        #region Helpers

        public delegate void OnBanEvent(object sender, Ban ban);
        public delegate void OnKickEvent(object sender, int connection, string reason, int duration);
        public event OnBanEvent OnBan;
        public event OnKickEvent OnKick;

        /// <summary>
        /// Kicks the given player for the given reason. Returns true if successful. Alias of <see cref="Rcon.StarboundRconClient.Kick(int, string)"/>.
        /// </summary>
        /// <param name="player">The player to kick.</param>
        /// <param name="reason">The reason to kick the player.</param>
        /// <returns></returns>
        public async Task<Rcon.RconResponse> Kick(Player player, string reason) => await Kick(player.Connection, reason);
        public async Task<Rcon.RconResponse> Kick(int connection, string reason)
        {
            //TODO: Make this return a kick response code
            //https://www.youtube.com/watch?v=Qku9aoUlTXA
            if (Rcon == null)
            {
                Logger.LogWarning("Cannot kick a player because the RCON server is not enabled.");
                return await Task.FromResult(new Rcon.RconResponse() { Success = false, Message = "Rcon not enabled" });
            }

            // Prevents a blocky error from Starbound.
            if (Connections.GetPlayer(connection) == null)
            {
                string error = $"Cannot kick a player (CID: {connection}) that isn't connected.";
                Logger.LogWarning(error);

                return await Task.FromResult(new Rcon.RconResponse() { 
                    Success = false, 
                    Message = error
                });
            }

            //Kick the connection
            var response = await Rcon.Kick(connection, reason);
            OnKick?.Invoke(this, connection, reason, -1);
            return response;
        }
        

        /// <summary>
        /// Kicks the player for the given reason and duration.
        /// </summary>
        /// <param name="player">The player to kick</param>
        /// <param name="reason">The reason of the kick</param>
        /// <param name="duration">The duration in seconds the kick lasts for.</param>
        /// <returns></returns>
        public async Task<Rcon.RconResponse> Kick(Player player, string reason, int duration) => await Kick(player.Connection, reason, duration);
        public async Task<Rcon.RconResponse> Kick(int connection, string reason, int duration)
        {
            if (duration <= 0) return new Rcon.RconResponse() { Success = false, Message = "Invalid duration." };
            var response = await Rcon.Ban(connection, reason, BanType.IP, duration);
            OnKick?.Invoke(this, connection, reason, duration);
            return response;
        }

        /// <summary>
        /// Bans a user for the given reason. Returns the ticket of the ban. Alias of <see cref="Settings.AddBan(Entities.Ban)"/>
        /// </summary>
        /// <param name="player">The player to ban</param>
        /// <param name="reason">The reason of the ban. This is formatted automatically. {ticket} will be replaced with the Ticket and {moderator} will be replaced with the moderator who added the ban.</param>
        /// <param name="moderator">The author of the ban</param>
        /// <param name="reload">Reloads the server with the ban</param>
        /// <param name="kick">Kicks the user from the server after the ban is added.</param>
        /// <returns></returns>
        public async Task<long> Ban(Player player, string reason, string moderator = "starwatch", bool reload = true, bool kick = true)
        {
            Logger.Log("Creating Player Ban: " + player);

            //Update the listing and get the correct user
            await Connections.RefreshListing();
            Player realP = Connections[player.Connection];

            //Make sure real p exists
            if (realP == null) realP = player;
            if (realP == null)
                throw new ArgumentNullException("Cannot ban the player because it does not exist.");

            //Ban the user
            var ban = new Ban(realP, reason, moderator);
            long ticket = await Ban(ban, reload);

            //Force to kick if requested
            if (kick)
            {
                if (Rcon == null)
                {
                    Logger.LogWarning("Cannot kick users after a ban because the rcon is disabled");
                }
                else
                {
                    Logger.Log("Kicking after ban");
                    var res = await Kick(player, ban.GetFormattedReason());
                    if (!res.Success)
                    {
                        Logger.LogError($"Error occured while trying to kick user: {res.Message}");
                    }
                }
            }

            return ticket;
        }

        /// <summary>
        /// Bans a user for the given reason. Returns the ticket of the ban. Alias of <see cref="Settings.AddBan(Entities.Ban)"/>
        /// <para>Ticket value will update if its not already set.</para>
        /// <para>BannedAt value will update if its not already set.</para>
        /// <para>Reason value will update, replacing all instances of {ticket} and {moderator} with the ticket id and the moderators name.</para>
        /// </summary>
        /// <param name="ban">The ban to add to the server.</param>  
        /// <param name="reload">Reloads the server with the ban</param>
        /// <returns></returns>
        public async Task<long> Ban(Ban ban, bool reload = true)
        {
            Logger.Log("Adding ban: " + ban);

            #region Try not to brick the server config with NetworkException

            if (!(ban.IP is null))
            {
                IPAddress parsedIp = null;
                bool parsedOk = IPAddress.TryParse(ban.IP, out parsedIp);

                if (!parsedOk)
                    throw new ArgumentException(nameof(ban.IP), "IP must be a valid IP Address");
            }
            
            #endregion

            //Add the ban, storing the tocket and saving the settings
            long ticket = await Configurator.AddBanAsync(ban);

            //Invoke the on ban
            OnBan?.Invoke(this, ban);

            //Reload the server, saving the configuration before we do.
            if (reload)
                await SaveConfigurationAsync(true);

            return ticket;
        }


        /// <summary>
        /// Gets the current statistics of the server
        /// </summary>
        /// <returns></returns>
        public async Task<Statistics> GetStatisticsAsync()
        {
            var usage = await GetMemoryUsageAsync();
            return new Statistics(this, usage);
        }

        /// <summary>
        /// Gets the current memory profile.
        /// </summary>
        /// <returns></returns>
        public Task<MemoryUsage> GetMemoryUsageAsync() { return Task.FromResult(_starbound.GetMemoryUsage()); }


        #endregion

        #region Running / Termination
        /// <summary>
        /// Forces the server to terminate. and waits for it to terminate.
        /// </summary>
        /// <returns></returns>
        public Task Terminate(string reason = null)
        {
            _terminate = true;
            LastShutdownReason = reason ?? LastShutdownReason;
            return Task.CompletedTask;
        }


        /// <summary>
        /// Runs the server, reading its input until its closed.
        /// </summary>
        /// <returns></returns>
		public async Task Run(System.Threading.CancellationToken cancellationToken)
        {
            //Make sure path exists
            string path = Path.Combine(WorkingDirectory, ExecutableName);
            if (!File.Exists(path))
            {
                Logger.LogError("Cannot start server as the executable '{0}' does not exist", path);
                return;
            }

            //Load the settings
            Logger.Log("Loading Settings...");

            //Make sure the configuration is valid
            if (!await Configurator.TryLoadAsync())
            {
                Logger.LogError("Cannot start server as the configuration is invalid, make sure you -import before use. see documentation for reference.", ConfigurationFile);
                return;
            }

            //Update the start time statistics.
            StartTime = DateTime.UtcNow;

            //Invoke the start event
            foreach (var m in _monitors)
            {
                try
                {
                    Logger.Log("OnServerPreStart :: {0}", m.Name);
                    await m.OnServerPreStart();
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "OnServerStart ERR :: " + m.Name + " :: {0}");
                }
            }

            //Saving the settings
            Logger.Log("Saving Settings before launch...");
            await SaveConfigurationAsync();

            #region Process Handling

            //Destroy the rcon reference. We will replace it with a new one later.
            Rcon = null;

            if (_starbound != null)
            {
                Logger.Log("Cleaning up old process");
                await _starbound.StopAsync();
                _starbound.Dispose();
            }

            //Start the process
            _terminate = false;
            _starbound = new SBProcess(path, WorkingDirectory, Logger.Child("SB"));
            _starbound.Exited += async () =>
            {
                Logger.Log("SB Process has exited!");

                //Tell us to terminate
                _terminate = true;

                //Validate the shutdown message, setting default if there isnt one
                LastShutdownReason = LastShutdownReason ?? "Server closed for unkown reason";
                foreach (var m in _monitors)
                {
                    try
                    {
                        Logger.Log("OnServerExit {0}", m.Name);
                        await m.OnServerExit(LastShutdownReason);
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e, "OnServerExit ERR - " + m.Name + ": {0}");
                    }
                }
            };

            Logger.Log("Staring Starbound Process");
            LastShutdownReason = null;
            _starbound.Start();

            //Try and initialize the rcon.
            if (Configurator.RconServerPort > 0)
            {
                Rcon = new Rcon.StarboundRconClient(this);
                OnRconClientCreated?.Invoke(this, Rcon);
                Logger.Log("Created RCON Client.");
            }


            #region Invoke the start event
            foreach (var m in _monitors)
            {
                try
                {
                    Logger.Log("OnServerStart :: {0}", m.Name);
                    await m.OnServerStart();
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "OnServerStart ERR :: " + m.Name + " :: {0}");
                }
            }
            #endregion

            try
            {

                //Handle the messages
                Logger.Log("Handling Messages");

                while (!_terminate && !cancellationToken.IsCancellationRequested)
                {
                    string[] logs = await _starbound.ReadStandardOutputAsync();
                    foreach(var l in logs)
                    {
                        _terminate = await ProcessLine(l);
                        if (_terminate) break;
                    }

                    //wait some tiny amount of time
                    await Task.Delay(100, cancellationToken);
                }

                //We have exited the loop, probably because we have been terminated
                Logger.Log("Left the read loop");
            }
            catch (Exception e)
            {
                Logger.LogError(e, "CRITICAL ERROR IN RUNTIME: {0}");
                LastShutdownReason = "Critical Error: " + e.Message + "\n" + e.StackTrace;
            }
            #endregion
            
            //We have terminated, so dispose of us properly
            Logger.Log("Attempting to terminate process just in case of internal error.");
            await _starbound.StopAsync();

            Logger.Log("Terminated Succesfully, disposing");
            _starbound.Dispose();
            _starbound = null;
        }

        /// <summary>
        /// Processes a line from the logs.
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private async Task<bool> ProcessLine(string line)
        {
            if (line == null)
            {
                Logger.Log("Read NULL from stdout");
                return true;
            }

            //Should we terminate?
            bool shouldTerminate = false;

            //Generate the message data
            Message msg = Message.Parse(this, line);
            if (msg != null)
            {
                //Process all the messages
                foreach (var m in _monitors)
                {
                    try
                    {
                        //Send it through the handlers
                        if (await m.HandleMessage(msg))
                        {
                            Logger.Log(m.Name + " has requested for us to terminate the loop");
                            shouldTerminate = true;
                            break;
                        }
                    }
                    catch (Exceptions.ServerShutdownException e)
                    {
                        Logger.Log($"Server Restart Requested by {m.Name}: {e.Message}");
                        LastShutdownReason = e.Message;
                        shouldTerminate = true;
                        break;
                    }
                    catch (AggregateException e)
                    {
                        if (e.InnerException is Exceptions.ServerShutdownException)
                        {
                            var sse = (Exceptions.ServerShutdownException) e.InnerException;
                            Logger.Log($"Server Restart Requested by {m.Name}: {sse.Message}");
                            LastShutdownReason = sse.Message;
                            shouldTerminate = true;
                            break;
                        }
                        else
                        {
                            //Catch any errors in the handler
                            Logger.LogError(e, "HandleMessage ERR - " + m.Name + ": {0}");
                        }
                    }
                    catch (Exception e)
                    {
                        //Catch any errors in the handler
                        Logger.LogError(e, "HandleMessage ERR - " + m.Name + ": {0}");
                    }
                }
            }

            //Return the state of terminating.
            return shouldTerminate;
        }
        #endregion

        #region Settings / Configurations

        /// <summary>
        /// Saves the starbound setting, making a backup of the previous one first.
        /// </summary>
        /// <param name="reload">Should RCON Reload be executed on success?</param>
        /// <returns>True if saved succesfully.</returns>
        public async Task<bool> SaveConfigurationAsync(bool reload = false)
        {
            Logger.Log("Writing configuration to file.");
            try
            { 

                //Export the settings and then serialize it
                Settings settings = await Configurator.ExportSettingsAsync();
                if (settings == null) return false;

                string json = JsonConvert.SerializeObject(settings, Formatting.Indented, new Serializer.UserAccountsSerializer());

                //Write all the text
                File.WriteAllText(ConfigurationFile, json);

                //Reload it
                if (reload)
                {
                    Debug.Assert(Rcon != null);
                    if (Rcon == null)
                    {
                        Logger.LogWarning("Cannot reload save because rcon does not exist.");
                    }
                    else
                    {
                        Logger.Log("Reloading the settings...");
                        await Rcon.ReloadServerAsync();
                    }
                }

                Logger.Log("Settings successfully saved.");
                return true;
            }
            catch(Exception e)
            {
                Logger.LogError(e, "Exception Occured while saving: {0}");
                return false;
            }
        }

        /// <summary>
        /// Sets and saves the starbound setting, making a backup of the previous one first.
        /// </summary>
        /// <param name="settings">The settings to save.</param>
        /// <param name="reload">Should RCON Reload be executed on success?</param>
        /// <returns>True if saved succesfully.</returns>
        [Obsolete("Do not directly save the settings", true)]
        public async Task<bool> SaveSettings(Settings settings, bool reload = false)
        {
            try
            {
                //Serialize it.
                Logger.Log("Serializing Settings...");
                string json = await Task.Run<string>(() => JsonConvert.SerializeObject(settings, Formatting.Indented, new Serializer.UserAccountsSerializer()));

                //Save it
                Logger.Log("Writing Settings...");
                File.WriteAllText(ConfigurationFile, json);

                //Reload it
                if (reload)
                {
                    Debug.Assert(Rcon != null);
                    if (Rcon == null)
                    {
                        Logger.LogWarning("Cannot reload save because rcon does not exist.");
                    }
                    else
                    {
                        Logger.Log("Reloading the settings...");
                        await Rcon.ReloadServerAsync();
                    }
                }

                Logger.Log("Settings successfully saved.");
                return true;
            }
            catch(Exception e)
            {
                Logger.LogError(e, "Exception Occured while saving: {0}");
                return false;
            }
        }
        #endregion


        public void Dispose()
        {
            //Dispose all the monitors
            foreach (var m in _monitors) m.Dispose();
            _monitors.Clear();

            //Force the server to close.
            if (_starbound != null)
            {
                _starbound.Dispose();
                _starbound = null;
            }
        }
    }
}


/*
 * 
		public T RegisterMonitor<T>() where T : Monitor => (T) RegisterMonitor(typeof(T));		
		public Monitor RegisterMonitor(Type type)
		{
			if (!typeof(Monitor).IsAssignableFrom(type))
				throw new ArrayTypeMismatchException("Cannot assign " + type.FullName + " to a monitor");

			var constructor = type.GetConstructor(new Type[] { typeof(Server), typeof(string) });
			var monitor = (Monitor) constructor.Invoke(new object[] { this, type.Name });

			monitors.Add(monitor);
			return monitor;
		}
		public Monitor[] RegisterMonitors(params Type[] types)
		{
			Monitor[] monitors = new Monitor[types.Length];
			for (int i = 0; i < types.Length; i++) monitors[i] = RegisterMonitor(types[i]);
			return monitors;
		}

	*/
