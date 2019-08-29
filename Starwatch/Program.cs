using Newtonsoft.Json;
using Starwatch.Entities;
using Starwatch.Util;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Starwatch
{
    struct SomeStructure
    {
        public int X;

        [JsonProperty("vAlUe")]
        public string Value { get; set; }
    }


    class Program : IDisposable
    {
        public static StarboundHandler Starwatch { get; private set; }

        private bool readLines = false;
        private CancellationTokenSource tokenSource;

        static void Main(string[] args)
        {
            Program program = new Program();
            program.RunStarwatch(args);

        }


        private Program()
        {
            tokenSource = new CancellationTokenSource();
            readLines = true;
        }

        private void RunStarwatch(string[] args)
        {
            Starwatch = new StarboundHandler(Configuration.FromFile("Starwatch.json"));
            
            var task = Task.Run(async () => { await Starwatch.Run(tokenSource.Token); }, tokenSource.Token);
            while (readLines && !(task.IsCompleted || task.IsCanceled))
            {
                string line = Console.ReadLine();
                if (line.StartsWith("rcon "))
                {
                    Console.WriteLine("Attempting Rcon...");
                    var response = Starwatch?.Server?.Rcon?.ExecuteAsync(line.Substring(5)).Result;

                    Console.WriteLine("Response: {0}", response.HasValue && response.Value.Success);
                    if (response.HasValue) Console.WriteLine(response.Value.Message);
                }
                else
                {
                    switch (line)
                    {
                        default:
                            Console.WriteLine("Unkown Command: {0}", line);
                            break;

                        case "restart":
                            Console.WriteLine("Restarting Server...");
                            Starwatch?.Server?.Terminate("CLI Restart").Wait();
                            break;

                        case "import":
                            var plevel = Starwatch.Logger.Level;
                            Starwatch.Logger.Level = Logging.Logger.LogLevel.None;
                            Console.WriteLine("Import Filepath [y/n]: ");
                            string importFilepath = Console.ReadLine();
                            if (!string.IsNullOrWhiteSpace(importFilepath))
                            {
                                if (File.Exists(importFilepath))
                                {
                                    Console.WriteLine("Triggering Import...");
                                    Starwatch.Logger.Level = plevel;
                                    Starwatch?.Server?.Configurator?.ImportSettingsAsync(importFilepath).Wait();
                                }
                                else
                                {
                                    Console.WriteLine("File " + importFilepath + " does not exist.");
                                }
                            }
                            Starwatch.Logger.Level = plevel;
                            Console.WriteLine("Done");
                            break;

                        case "process-worlds":
                            if (Starwatch == null || Starwatch.Server == null)
                            {
                                Console.WriteLine("Cannot process all worlds because the server doesnt exist.");
                                break;
                            }

                            Console.WriteLine("Processing All Worlds...");
                            foreach(var filepath in Directory.EnumerateFiles(Starwatch.Server.StorageDirectory + "/universe/", "*.world"))
                            {
                                //Generate the world
                                Console.WriteLine("Processing " + filepath);
                                var world = World.ParseFilename(filepath) as CelestialWorld;

                                //Save the world
                                try
                                {
                                    world.GetDetailsAsync(Starwatch.Server).Wait();
                                    Console.WriteLine(" = " + world.Details?.Name);
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine("ERROR: " + e.Message);
                                }
                            }

                            Console.WriteLine("DONE");
                            break;

                        case "exit":
                        case "quit":
                        case "stop":
                            Console.WriteLine("Cancelling Token...");
                            Starwatch.KeepAlive = false;
                            Starwatch?.Server?.Terminate("CLI Stop").Wait();
                            tokenSource.Cancel();
                            readLines = false;
                            break;
                    }
                }
            }

            Console.WriteLine("Waiting for task to finish... {0}", task.IsCanceled);
            task.Wait();

            Console.WriteLine("Goodbye!\nPress any key to exit...");
            Console.ReadKey();
        }

        private void TestJSON()
        {
            Configuration config = new Configuration(new Newtonsoft.Json.Linq.JObject());
            config?.SetKey("test", 1);
            config?.SetKey("apple", "sauce");
            config?.SetKey("owo", false);
            config?.SetKey("struct", new SomeStructure() { X = 194, Value = "Hello World" });

            Configuration child = config?.GetConfiguration("first_child");
            child?.SetKey("mango", "apple");
            child?.SetKey("other", new SomeStructure() { X = 999, Value = "OwO" });

            var json = config?.ToJson(Formatting.Indented);
            Console.WriteLine(json);
            Console.ReadKey(true);


            var config2 = Configuration.FromJson(json);

            Console.WriteLine(json);
            Console.ReadKey(true);
        }

        public void Dispose()
        {
            if (Starwatch != null)
            {
                Console.WriteLine("Terminating Server");
                Starwatch.KeepAlive = false;
                if (Starwatch.Server != null) Starwatch.Server.Dispose();
            }

            if (tokenSource != null)
            {
                Console.WriteLine("Disposing Token");
                tokenSource.Dispose();
                tokenSource = null;
            }
        }
    }
}

/*
 *   Random rand = new Random();
                System.Threading.Thread thread = new System.Threading.Thread(() => {
                    while (true)
                    {
                        int time = rand.Next(1500, 20000);
                        Console.WriteLine("Thread Sleep: {0}", time);
                        System.Threading.Thread.Sleep(time);
                        Console.WriteLine("Thread Terminate");
                        server.Terminate().Wait();
                        Console.WriteLine("Thread Done");
                    }
                });

                thread.Start();
				
*/