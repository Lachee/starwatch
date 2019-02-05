using Newtonsoft.Json;
using Starwatch.Entities;
using Starwatch.Logging;
using Starwatch.Monitoring;
using Starwatch.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        public StarboundHandler Starwatch { get; private set; }
        public Logger Logger { get; private set; }

        public string SamplePath { get; set; }
        public int SampleMinDelay { get; set; } = 250;
        public int SampleMaxDelay { get; set; } = 250 * 10;

        public string StorageDirectory => WorkingDirectory + @"\..\storage";
        public string WorkingDirectory { get; }
        public string ExecutableName { get; }
        public string ConfigurationFile { get; }

        /// <summary>
        /// Handles the state tracking of players connected to the server
        /// </summary>
        public Connections Connections { get; private set; }


        /// <summary>
        /// The settings for the server
        /// </summary>
        public Settings Settings { get; private set; }

        /// <summary>
        /// The configuration for the server
        /// </summary>
        public Configuration Configuration { get; private set; }

        /// <summary>
        /// The RCON Client for the server.
        /// </summary>
        public Rcon.StarboundRconClient Rcon { get; private set; }

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
        public bool IsRunning => ProcessExists && !_terminate && StartTime > EndTime;

        private bool _terminate = false;
        public bool ProcessExists => _process != null;
        private Process _process;
        private System.Threading.CancellationTokenSource _processAbortTokenSource;
        private System.Threading.SemaphoreSlim _processSemaphore = new System.Threading.SemaphoreSlim(1, 1);

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

        }

        /// <summary>
        /// Loads all the monitors from the config. This must be called for anything useful to happen
        /// </summary>
        /// <returns></returns>
        public async Task LoadMonitors()
        {
            //Unload all previous monitors
            await UnloadMonitors();

            //Load up the connection
            Connections = new Connections(this);
            _monitors = new List<Monitor>() { Connections };

            //Register all the objects
            foreach (string monitor in Configuration.GetObject<string[]>("monitors", new string[0]))
            {
                var type = Type.GetType(monitor);

                var constructor = type.GetConstructor(typeof(Server));
                var instance = (Monitoring.Monitor)constructor.Invoke(this);
                await instance.Initialize();
                _monitors.Add(instance);
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

        #region Helpers

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

            //Kick the connection
            return await Rcon.Kick(connection, reason);
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
            return await Rcon.Ban(connection, reason, BanType.IP, duration);
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
        public async Task<long> Ban(Player player, string reason, string moderator = "starwawtch", bool reload = true, bool kick = true)
        {
            Logger.Log("Creating Player Ban: " + player);

            //Update the listing and get the correct user
            await Connections.RefreshListing();
            Player realP = Connections[player.Connection];

            //Ban the user
            long ticket = await Ban(new Ban(realP, reason, moderator), reload);

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
                    var res = await Kick(player, reason);
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

            //Add the ban, storing the tocket and saving the settings
            long ticket = Settings.AddBan(ban);
            if (!await SaveSettings())
            {
                Logger.LogError("Failed to save the settings for banning!");
                return -1;
            }

            //Reload the server
            if (reload)
            {
                if (Rcon == null)
                {
                    Logger.LogWarning("Cannot reload after a ban because the rcon is disabled");
                }
                else
                {
                    Logger.Log("Reloading after ban");
                    await Rcon.ReloadServerAsync();
                }
            }

            return ticket;
        }

        /// <summary>
        /// Gets the memory usage.
        /// </summary>
        /// <returns></returns>
        public long GetMemoryUsage()
        {
            //TODO: Use the semaphore for this.
            return ProcessExists ? _process.WorkingSet64 : 0;
        }       
        
        /// <summary>
        /// Gets the peak usage.
        /// </summary>
        /// <returns></returns>
        public long GetPeakMemoryUsage()
        {
            //TODO: Use the semaphore for this.
            return ProcessExists ? _process.PeakWorkingSet64 : 0;
        }

        /// <summary>
        /// Gets the current statistics of the server
        /// </summary>
        /// <returns></returns>
        public Statistics GetStatistics() => new Statistics(this);
        #endregion

        #region Running / Termination
        /// <summary>
        /// Forces the server to terminate. and waits for it to terminate.
        /// </summary>
        /// <returns></returns>
        public async Task Terminate()
        {
            if (_processAbortTokenSource != null)
            {
                Logger.Log("Terminating process (via token)");
                _processAbortTokenSource.Cancel();
            }
            else
            {
                Logger.Log("Terminating process (via kill)");
                await Kill();
            }
        }

        private async Task Kill()
        { 
            await _processSemaphore.WaitAsync();
            try
            {
                if (_process == null)
                    return;

                //Dispose the token
                if (_processAbortTokenSource != null)
                {
                    Logger.Log("Disposing abort token source...");
                    _processAbortTokenSource.Dispose();
                    _processAbortTokenSource = null;
                }

                Logger.Log("Killing & Waiting for process...");
                if (!_process.HasExited) _process.Kill();
                _process.WaitForExit();

                Logger.Log("Disposing of process...");
                _process.Dispose();
                _process = null;

                Logger.Log("Process Disposed.");
                EndTime = DateTime.UtcNow;
            }
            finally
            {
                _processSemaphore.Release();
            }

            //save the settings
            await SaveSettings();
        }

        /// <summary>
        /// Runs the server, reading its input until its closed.
        /// </summary>
        /// <returns></returns>
		public async Task Run(System.Threading.CancellationToken cancellationToken)
        {
            //We cannot run because the process already exists
            if (ProcessExists)
            {
                Logger.LogWarning("Cannot run process as it is already running");
                return;
            }

            //Make sure path exists
            string path = Path.Combine(WorkingDirectory, ExecutableName);
            if (!File.Exists(path))
            {
                Logger.LogError("Cannot start server as the executable '{0}' does not exist", path);
                return;
            }

            //Make sure config exists
            if (!File.Exists(ConfigurationFile))
            {
                Logger.LogError("Cannot start server as the configuration '{0}' does not exist", ConfigurationFile);
                return;
            }

            //Load the settings
            Logger.Log("Loading Settings...");
            string settingContents = File.ReadAllText(ConfigurationFile);
            Settings = JsonConvert.DeserializeObject<Settings>(settingContents, new Serializer.UserAccountsSerializer());

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
            await SaveSettings();

            if (string.IsNullOrEmpty(SamplePath))
            {
                //Destroy the rcon reference. We will replace it with a new one later.
                Rcon = null;

                //Create the process
                await _processSemaphore.WaitAsync();
                try
                {
                    //Create a new token source for canceling
                    if (_processAbortTokenSource == null)
                    {
                        Logger.Log("Creating new abort token source...");
                        _processAbortTokenSource = new System.Threading.CancellationTokenSource();
                    }

                    //Create the process
                    Logger.Log("Creating new process...");
                    _process = new Process()
                    {
                        StartInfo = new ProcessStartInfo()
                        {
                            WorkingDirectory = WorkingDirectory,
                            FileName = path,
                            RedirectStandardOutput = true,
                            UseShellExecute = false
                        }
                    };
                }
                finally
                {
                    _processSemaphore.Release();
                }

                //Log the start of the process
                Logger.Log("Staring Starbound Process");
                bool success = _process.Start();
                if (success)
                {
                    //Try and initialize the rcon.
                    if (Settings.RunRconServer)
                    {
                        Rcon = new Rcon.StarboundRconClient(this);
                        OnRconClientCreated?.Invoke(this, Rcon);
                        Logger.Log("Created RCON Client.");
                    }

                    //Invoke the start event
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

                    //Handle the messages
                    Logger.Log("Handling Messages");

                    //Generate a new token source
                    _process.BeginOutputReadLine();
                    _process.OutputDataReceived += async (sender, args) =>
                    {
                        _terminate = await ProcessLine(args.Data);
                        if (_terminate) await Terminate();
                    };

                    
                    //Do the cancel
                    try { await Task.Delay(-1, _processAbortTokenSource.Token); } catch (TaskCanceledException) { }

                    /*
                    //Setup the terminate value and being processing messages
                    _terminate = false;
                    while (!_terminate && !cancellationToken.IsCancellationRequested)
                    {
                        BufferedStream buffstream = new BufferedStream(_process.StandardOutput.BaseStream);
                        buffstream.ReadAsync()
                        if (cancellationToken.IsCancellationRequested)
                        {
                            Logger.LogWarning("Leaving read because of cancellation token!");
                            _terminate = true;
                        }
                        else
                        {
                            _terminate = await ProcessLine(data);
                        }

                        //Loop back again, trying to read messages again
                    }
                    */



                    //We have exited the loop, probably because we have been terminated
                    Logger.Log("Left the read loop");
                }
                else
                {
                    Logger.Log("Failed to start the starbound process for some reason.");
                }
            }
            else
            {
                //Invoke the start event
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

                Logger.Log("Reading Predefined Log");
                Random random = new Random();
                foreach (var line in File.ReadLines(SamplePath))
                {
                    if (line.Length < 15) continue;
                    if (cancellationToken.IsCancellationRequested) break;

                    //Process the read line
                    _terminate = await ProcessLine(line.Substring(15));

                    //Break out of the reading
                    if (_terminate) break;

                    //Wait a bit
                    await Task.Delay(random.Next(SampleMinDelay, SampleMaxDelay));
                }

                Logger.Log("Finished reading prefined log");
            }

            //Invoke exit event
            foreach (var m in _monitors)
            {
                try
                {
                    Logger.Log("OnServerExit {0}", m.Name);
                    await m.OnServerExit();
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "OnServerExit ERR - " + m.Name + ": {0}");
                }
            }

            //We have terminated, so dispose of us properly
            Logger.Log("Attempting to terminate process just in case of internal error.");
            await Kill();
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
        public async Task<bool> SaveSettings(bool reload = false) => await SaveSettings(Settings, reload);

        /// <summary>
        /// Sets and saves the starbound setting, making a backup of the previous one first.
        /// </summary>
        /// <param name="settings">The settings to save.</param>
        /// <param name="reload">Should RCON Reload be executed on success?</param>
        /// <returns>True if saved succesfully.</returns>
        public async Task<bool> SaveSettings(Settings settings, bool reload = false)
        {
            try
            {
                //Set the settings
                this.Settings = settings;

                //Serialize it.
                Logger.Log("Serializing Settings...");
                string json = await Task.Run<string>(() => JsonConvert.SerializeObject(Settings, Formatting.Indented, new Serializer.UserAccountsSerializer()));

                //Backup the previous one
                if (File.Exists(ConfigurationFile))
                {
                    Logger.Log("Backing up previous...");
                    File.Copy(ConfigurationFile, ConfigurationFile + "." + DateTime.UtcNow.ToShortDateString().Replace('/', '-') + ".bak", true);
                }

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
            foreach (var m in _monitors) m.Dispose();
            _monitors.Clear();

            if (_process != null)
            {
                _process.Kill();
                _process.Dispose();
                _process = null;
            }

            if (_processSemaphore != null)
            {
                _processSemaphore.Dispose();
                _processSemaphore = null;
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
