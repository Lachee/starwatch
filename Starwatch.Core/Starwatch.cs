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
    public class StarboundHandler
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

            //Initialize the region export directory
            PythonScriptsDirectory = Configuration.GetString("python_parsers", "Resources/py");
            
            //Initialize the default logger
            this.Logger = new Logger("SWC");

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

        public async Task Run(CancellationToken cancellationToken)
        {
            try
            {
                if (DbContext == null)
                {
                    Logger.Log("Initializing Database...");
                    var settings = Configuration.GetObject<ConnectionSettings>("SQL", new ConnectionSettings()
                    {
                        Host = "localhost",
                        Database = "starwatch",
                        Username = "root",
                        Password = "rootpass",
                        Prefix = "sb_",
                        DefaultImport = "Resources/starwatch.sql"
                    });
                    DbContext = new DbContext(settings, Logger.Child("SQL"));

                    if (!string.IsNullOrWhiteSpace(settings.DefaultImport))
                        await DbContext.ImportSqlAsync(settings.DefaultImport);
                }
                
                //Initialize the API first
                if (ApiHandler == null)
                {
                    Logger.Log("Initializing API...");
                    ApiHandler = new API.ApiHandler(this, Configuration.GetConfiguration("api"));
                    await ApiHandler.Start();
                }

                //Update the list of available monitors
                Configuration.SetKey("available_monitors", BuildMonitorList(System.Reflection.Assembly.GetAssembly(typeof(Monitoring.Monitor))));
                Configuration.Save();

                //Initialize some intial settings and then run the server
                KeepAlive = true;
                while (KeepAlive && !cancellationToken.IsCancellationRequested)
                {
                    //Load the server if its null
                    if (Server == null)
                    {
                        string[] monitors = Configuration.GetObject<string[]>("available_monitors", null);
                        if (monitors == null)
                        {
                            Logger.LogError("Monitors are not setup. Creating inital array then aborting!");
                            KeepAlive = false;
                        }

                        //Initialize the server
                        Server = await InitializeServer(Configuration.GetConfiguration("server"));

                        //Save our configuration
                        Configuration.Save();
                    }


                    //Run the server
                    if (KeepAlive && !cancellationToken.IsCancellationRequested)
                    {
                        Logger.Log("Running the server...");
                        await Server.Run(cancellationToken);
                    }
                }

                //Terminate the server
                if (Server != null)
                {
                    Logger.Log("Terminating and closing server...");
                    //await Server.Terminate("Cancellation Token was aborted");
                }

                //Terminate the API
                if (ApiHandler != null)
                {
                    Logger.Log("Terminating the API handler...");
                    await ApiHandler.Stop();
                }

                //Save our configuration
                Configuration.Save();
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Uncaught exception occured! {0}");
            }
            finally
            {
                //Close the database
                if (DbContext != null)
                {
                    DbContext.Dispose();
                    DbContext = null;
                }

                //Finally close the server
                if (Server != null)
                {
                    Logger.Log("Disposing the server...");
                    Server.Dispose();
                    Server = null;
                    Logger.Log("Finished disposing the server.");
                }
            }
        }

        private async Task<Server> InitializeServer(Configuration configuration)
        {
            //Prepare the server object
            var server = new Server(this, configuration);
            await server.LoadAssemblyMonitorsAsync(System.Reflection.Assembly.GetAssembly(typeof(Server)));

            //Return the server
            return server;
        }

        private string[] BuildMonitorList(System.Reflection.Assembly assembly) 
        {
           return assembly.GetTypes().Where(t => !t.IsAbstract && typeof(Monitoring.Monitor).IsAssignableFrom(t) && t != typeof(Connections)).Select(t => t.FullName).ToArray();
        }
        
    }
}
