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
using Starwatch.Entities;
using Starwatch.Util;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Starwatch
{
    class Program
    {
        /// <summary>
        /// Static reference to the current starbound handler
        /// </summary>
        public static StarboundHandler Starwatch { get; private set; }

        /// <summary>
        /// Configuration File for the Starwatch
        /// </summary>
        public static string ConfigurationFile { get; private set; } = "Starwatch.json";

        /// <summary>
        /// Task for starwatch
        /// </summary>
        private static Task _starwatchTask;

        static void Main(string[] args)
        {
            string configurationImportFile = null;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-import":
                    case "-i":
                        configurationImportFile = args[++i];
                        break;

                    case "-config":
                    case "-c":
                        ConfigurationFile = args[++i];
                        break;

        private Program()
        {
            tokenSource = new CancellationTokenSource();
            readLines = true;
        }
            }

        private void RunStarwatch(string[] args)
        {
            Starwatch = new StarboundHandler(Configuration.FromFile("Starwatch.json"));
            
            var task = Task.Run(async () => { await Starwatch.Run(tokenSource.Token); }, tokenSource.Token);
            while (readLines && !(task.IsCompleted || task.IsCanceled))
            {
                //Import the previous starbound configuration.
                if (!string.IsNullOrEmpty(configurationImportFile))
                {
                    Console.WriteLine("Preparing Initializer...");
                    Starwatch.Initialize().Wait();

                    Console.WriteLine("Importing...");
                    Starwatch.Server.Configurator.ImportSettingsAsync(configurationImportFile).Wait();

                    Console.WriteLine("Deinitializing...");
                    Starwatch.Deinitialize().Wait();

                    Console.WriteLine("Done!");
                    return;
                }

                //Regular Starwatch instance
                var context =  Starwatch.Run(cancellationSource.Token).ConfigureAwait(false);

                //infinite loop to proces lines
                bool process = true;
                while (process && !context.GetAwaiter().IsCompleted)
                {
                    string line = Console.ReadLine();
                    switch (line)
                    {
                        case "stop":
                        case "exit":
                        case "abort":
                        case "quit":
                            Console.WriteLine("Cancelling Token...");
                            Starwatch.KeepAlive = false;
                            Starwatch?.Server?.Terminate("CLI Stop").Wait();
                            cancellationSource.Cancel();
                            process = false;
                            break;

                        case "restart":
                            Starwatch?.Server.Terminate("CLI Restart...");
                            break;
                    }
                }

                //We exited, so lets close things down
                Console.WriteLine("Done!");
            }
        }

    }

}
