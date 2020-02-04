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
using Starwatch.Database;
using Starwatch.Logging;
using Starwatch.Starbound;
using Starwatch.Util;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Starwatch
{
    public class StarboundHandler : IDisposable
    {
        public volatile bool KeepAlive = true;

        public Logger Logger { get; private set; }
        public Server Server { get; private set; }
        public Configuration Configuration { get; private set; }
        public DbContext DbContext { get; private set; }
        public API.ApiHandler ApiHandler { get; private set; }
        public string PythonScriptsDirectory { get; private set; }

        public StarboundHandler(Configuration configuration)
        {
            this.Configuration = configuration;

            //Initialize the threaded logger
            string outputFilename = Configuration.GetString("output", "");
            if (!string.IsNullOrEmpty(outputFilename))
            {
                OutputLogQueue.Initialize(outputFilename, Configuration.GetBool("output_append", false));
            }

            //Initialize the default logger
            this.Logger = new Logger("SWC");
            Logger.Log("Logger Initialized");

            //Setup the database
            var settings = Configuration.GetObject<ConnectionSettings>("SQL", new ConnectionSettings()
            {
                Host = "localhost",
                Database = "starwatch",
                Username = "root",
                Password = "rootpass",
                Prefix = "sb_",
            });
            DbContext = new DbContext(settings, Logger.Child("SQL"));

            //Make sure we actually encrypt
            if (string.IsNullOrEmpty(settings.Passphrase))
                throw new Exception("SQL Passphrase cannot be empty. This is used to encrypt sensitive player data!");

            //Initialize the region export directory
            PythonScriptsDirectory = Configuration.GetString("python_parsers", "Resources/py");

            //Save the configuration with the defaults
            Configuration.Save();

            //Setup the unhandled exception handler
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += new UnhandledExceptionEventHandler(UnhandledExceptionHandler);
        }

        private void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            Logger.LogError("=== CRITICAL ERROR: UNHANDLED EXCEPTION ===");
            Logger.LogError("Terminating: " + e.IsTerminating);
            Logger.LogError(e.ExceptionObject as Exception, "Exception: {0}");
        }

        /// <summary>
        /// Initializes the servers
        /// </summary>
        /// <returns></returns>
        public async Task Initialize()
        {
            //Initialize the API first
            if (ApiHandler == null)
            {
                Logger.Log("Initializing API...");
                ApiHandler = new API.ApiHandler(this, Configuration.GetConfiguration("api"));
                await ApiHandler.Start();
            }

            //Load the server if its null
            if (Server == null)
            {
                //Initialize the server
                Server = new Server(this, Configuration.GetConfiguration("server"));
                await Server.LoadAssemblyMonitorsAsync(System.Reflection.Assembly.GetAssembly(typeof(Server)));
                
                //Save our configuration
                Configuration.Save();
            }
        }

        /// <summary>
        /// Deinitializes the server and API
        /// </summary>
        /// <returns></returns>
        public async Task Deinitialize()
        {
            //Terminate the server
            if (Server != null)
            {
                Logger.Log("Terminating and closing server...");
                if (Server.IsRunning) await Server.Terminate();
            }

            //Terminate the API
            if (ApiHandler != null)
            {
                Logger.Log("Terminating the API handler...");
                await ApiHandler.Stop();
            }
        }

        /// <summary>
        /// Runs all the servers after initializing
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task Run(CancellationToken cancellationToken)
        {
            try
            {
                Logger.Log("Running Starwatch");

                //Initialize some intial settings and then run the server
                KeepAlive = true;
                while (KeepAlive && !cancellationToken.IsCancellationRequested)
                {
                    //Rerun the initialize
                    await Initialize();

                    //Run the server
                    if (KeepAlive && !cancellationToken.IsCancellationRequested)
                    {
                        Logger.Log("Running the server...");
                        await Server.Run(cancellationToken);
                    }
                }

                //Deinitialize
                await Deinitialize();

                //Save our configuration
                Configuration.Save();
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Uncaught exception occured! {0}");
            }
            finally
            {
                //Call our own dispose to make sure nothing is left running
                Dispose();
            }
        }

        public void Dispose()
        {
            //Disable the keep alive
            KeepAlive = false;

            //Dispose the API
            if (ApiHandler != null)
            {
                ApiHandler.Stop().Wait();
                ApiHandler = null;
            }

            //Dispose the server
            if (Server != null)
            {
                Server.Dispose();
                Server = null;
            }

            //Dispose the DB
            if (DbContext != null)
            {
                DbContext.Dispose();
                DbContext = null;
            }
        }
    }
}
