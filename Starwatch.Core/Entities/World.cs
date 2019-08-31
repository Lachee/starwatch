using Newtonsoft.Json;
using Starwatch.Starbound;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Starwatch.Entities
{
    //ClientShipWorld:bdcb81d986d009e4915d42f3b9e55f20
    //CelestialWorld:17:17:-198600707:10
    //CelestialWorld:-55603946:357637721:-294801013:10=4934.94.1173.41
    //InstanceWorld:outpost:-:-=outpost
    //InstanceWorld:museum:-:-=128.873
    //InstanceWorld:outpost:-:-
    //InstanceWorld:penguinship:78e95c7bb5db444b14c1bf8764aea423:1

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public abstract class World
    {
        private const string PatternWhereami = @"(?'CelestialWorld'CelestialWorld:(?'x'-?\d*):(?'y'-?\d*):(?'system'-?\d*):(?'planet'\d*)(:(?'moon'\d*))?)|(?'InstanceWorld'InstanceWorld:(?'type'\w*):(?'uid'[a-z0-9]{32}):(?'val'\d*))";
        private const string PatternFile = @"^(?'unique'unique-(?'type'\w*)-(?'uid'\w{32})-(?'val'\d*)\.world)|^(?'coord'(?'x'-?\d*)_(?'y'-?\d*)_(?'system'-?\d*)_(?'planet'\d*)(_(?'moon'\d*))?\.world)";

        private static readonly Regex RegexWhereami = new Regex(PatternWhereami, RegexOptions.Compiled);
        private static readonly Regex RegexFile = new Regex(PatternFile, RegexOptions.Compiled);

        public abstract string Whereami { get; }
        public abstract string Filename { get; }

        [JsonIgnore]
        public virtual string FileExtension => Filename == null ? null : System.IO.Path.GetExtension(Filename);

        [JsonIgnore]
        public virtual string JsonFilename => Filename + ".json";

        /// <summary>
        /// Parses the whereami to a world object.
        /// </summary>
        /// <param name="whereami"></param>
        /// <returns></returns>
        public static World ParseWhereami(string whereami)
        {
            //Seperate the parts
            string[] subs = whereami.Split(':');
            if (subs.Length < 2 || subs.Length > 6) return null;
            if (subs.Length == 3) return new SystemWorld(new string[] { "SystemWorld", subs[0], subs[1], subs[2] });

            //determin the type
            switch (subs[0])
            {
                default: return null;
                case "CelestialWorld": return new CelestialWorld(subs);
                case "InstanceWorld": return new InstanceWorld(subs);
                case "ClientShipWorld": return new ClientShipWorld(subs);
            }
        }

        /// <summary>
        /// Parses the filename to a world object
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static World ParseFilename(string filename)
        {
            //Trim it a bit so its only the important data.
            filename = System.IO.Path.GetFileName(filename);
            filename = filename.Replace(':', '_');

            //Check if its a instance world
            bool isInstance = filename.StartsWith("unique");

            //Some basic manipulation of the filename so we dont have to do as much assignments later on.
            //Specifically, we are going to add the first part so when we split it no futher work is required.
            if (isInstance)
                filename = filename.Replace("unique-", "InstanceWorld-");
            else
                filename = filename.StartsWith("CelestialWorld_") ? filename : "CelestialWorld_" + filename;

            //Split the parts into subs
            string[] subs = filename.Split(isInstance ? '-' : '_');

            //Remove the extension if any
            string ending = subs[subs.Length - 1];
            if (ending.EndsWith(".world"))
            {
                //Remove the .world
                ending = ending.Substring(0, ending.Length - 6);
                subs[subs.Length - 1] = ending;
            }
            else if (ending.EndsWith(".system"))
            {
                //Remove the .system
                ending = ending.Substring(0, ending.Length - 7);
                subs[subs.Length - 1] = ending;
            }

            //We have 3 coords, it must be a system.
            if (!isInstance && subs.Length == 4)
                subs[0] = "SystemWorld";

            //return the correct object
            if (isInstance) return new InstanceWorld(subs);
            if (subs.Length == 4) return new SystemWorld(subs);
            return new CelestialWorld(subs);
        }

        /// <summary>
        /// Parses the identifier too a World. The identifier can be either the result from a whereami or a filename ending with .world. Returns null if the pattern is not valid.
        /// </summary>
        /// <param name="identifier">The string identifier of the world, either the result of /whereami or the filename ending with .world</param>
        /// <returns></returns>
        public static World Parse(string identifier)
        {
            bool isFilename = identifier.EndsWith(".world") || identifier.EndsWith(".system") || !identifier.Contains(":");
            if (!isFilename)
            {
                return ParseWhereami(identifier);
            }
            else
            {
                return ParseFilename(identifier);
            }
        }

        public override string ToString() => this.Whereami;
        public override int GetHashCode() => this.Whereami.GetHashCode();
        public override bool Equals(object obj)
        {
            var world = obj as World;
            if (world == null) return false;
            return this.Whereami.Equals(world.Whereami);
        }

        /// <summary>
        /// Gets the absolute file path. Only available for worlds with a filename.
        /// </summary>
        /// <param name="server"></param>
        /// <returns></returns>
        public string GetAbsolutePath(Server server)
        {
            if (string.IsNullOrWhiteSpace(Filename))
                throw new InvalidOperationException("Cannot export data for a world that does not have a filename.");

            return Path.Combine(server.StorageDirectory, "universe/", Filename);
        }

        /// <summary>
        /// Gets the absolute path for the json.
        /// </summary>
        /// <param name="server"></param>
        /// <returns></returns>
        public string GetAbsoluteJsonPath(Server server)
        {
            if (string.IsNullOrWhiteSpace(Filename))
                throw new InvalidOperationException("Cannot export data for a world that does not have a filename.");

            return Path.Combine(server.StorageDirectory, "universe/", JsonFilename);
        }

        /// <summary>
        /// Exports the JSON data for the world and returns the path it was exported too. Only available for worlds with a valid Filename.
        /// </summary>
        /// <param name="server">The server to export from</param>
        /// <param name="overwrite">Overwrite the file if it already exists.</param>
        /// <returns></returns>
        public async Task<string> ExportJsonDataAsync(Server server, bool overwrite = false)
        {
            if (string.IsNullOrWhiteSpace(Filename))
                throw new InvalidOperationException("Cannot export data for a world that does not have a filename.");

            if (!FileExtension.Equals(".world", StringComparison.InvariantCultureIgnoreCase))
                throw new InvalidOperationException("Cannot export data for a non-world. Filename requires .world extension");

            string inputFile = GetAbsolutePath(server);
            string outputFile = Path.Combine(server.StorageDirectory, "universe/", JsonFilename);

            File.WriteAllText(server.Starwatch.PythonScriptsDirectory + "/tmp.txt", "TMP");

            if (!System.IO.File.Exists(inputFile))
                throw new System.IO.FileNotFoundException("Failed to find the world's file.", inputFile);

            if (!overwrite && System.IO.File.Exists(outputFile))
                return outputFile;

            //Run the process, waiting for it to exit.
            return await Task.Run(() => {
                var file = inputFile.Replace("\\", "/");
                var p = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("python", $"-m starbound.clidump \"{file}\"")
                {
                    WorkingDirectory = server.Starwatch.PythonScriptsDirectory,
                    RedirectStandardOutput = true
                });

                string content = p.StandardOutput.ReadToEnd();
                p.WaitForExit();

                p.Dispose();

                File.WriteAllText(outputFile, content);
                return outputFile;
            });
        }
    }

    public partial class SystemWorld : World
    {
        /// <summary>
        /// The X coordinate
        /// </summary>
        public long X { get; set; }
        /// <summary>
        /// The Y coordinate
        /// </summary>
        public long Y { get; set; }
        /// <summary>
        /// The Z coordinate
        /// </summary>
        public long Z { get; set; }

        /// <summary>
        /// Creates a new instance of the world.
        /// </summary>
        public SystemWorld() { }

        /// <summary>
        /// Creates a new instance of the world, parsing the subs from the whereami information.
        /// </summary>
        /// <param name="subs"></param>
        internal SystemWorld(string[] subs)
        {
            if (subs.Length != 4) throw new ArgumentOutOfRangeException("subs", subs.Length, "SystemWorld subs constructor requires exactly 4 elements: SystemWorld, X, Y, Z");
            if (subs[0] != "SystemWorld") throw new ArgumentException("The first element of the subs must be equal to SystemWorld");

            X = long.Parse(subs[1]);
            Y = long.Parse(subs[2]);
            Z = long.Parse(subs[3]);
        }

        public override string FileExtension => ".system";
        public override string Filename => $"{X}_{Y}_{Z}.system";
        public override string Whereami => $"{X}:{Y}:{Z}";

        public override int GetHashCode() => Whereami.GetHashCode();

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj is SystemWorld world)
                return this.X == world.X && this.Y == world.Y && this.Z == world.Z;
            return false;
        }
    }

    public class ClientShipWorld : World
    {
        /// <summary>
        /// The UUID of the player that owns the shipworld.
        /// </summary>
        public string UUID { get; set; }

        /// <summary>
        /// Creates a new instance of the world.
        /// </summary>
        public ClientShipWorld() { }

        /// <summary>
        /// Creates a new instance of the world, parsing the subs from the whereami information.
        /// </summary>
        /// <param name="subs"></param>
        internal ClientShipWorld(string[] subs)
        {
            //ClientShipWorld:bdcb81d986d009e4915d42f3b9e55f20
            if (subs.Length != 2) throw new ArgumentOutOfRangeException("subs", subs.Length, "ClientShipWorld subs constructor requires exactly 2 elements");
            if (subs[0] != "ClientShipWorld") throw new ArgumentException("The first element of the subs must be equal to ClientShipWorld");
            UUID = subs[1];
        }

        public override string FileExtension => null;
        public override string Filename => null;
        public override string Whereami => $"ClientShipWorld:{UUID}";

    }

    public class InstanceWorld : World
    {
        /// <summary>
        /// The type of the instance world
        /// </summary>
        public string Type { get; set; }
        
        /// <summary>
        /// The unique id of the instance world
        /// </summary>
        public string UID { get; set; }

        /// <summary>
        /// Some value of the instance world.
        /// </summary>
        public string Val { get; set; }
        
        /// <summary>
        /// Creates a new instance of the world.
        /// </summary>
        public InstanceWorld() { }
        
        /// <summary>
        /// Creates a new instance of the world, parsing the subs from the whereami information.
        /// </summary>
        /// <param name="subs"></param>
        internal InstanceWorld(string[] subs)
        {
            //InstanceWorld:outpost:-:-=outpost
            //InstanceWorld:museum:-:-=128.873
            //InstanceWorld:outpost:-:-
            //InstanceWorld:penguinship:78e95c7bb5db444b14c1bf8764aea423:1

            if (subs.Length != 4) throw new ArgumentOutOfRangeException("subs", subs.Length, "InstanceWorld subs constructor requires exactly 4 elements");
            if (subs[0] != "InstanceWorld") throw new ArgumentException("The first element of the subs must be equal to InstanceWorld");

            //Remove the =4934.94.1173.41 of the last element
            //int ioEquals = subs[subs.Length - 1].IndexOf('=');
            //if (ioEquals >= 0) subs[subs.Length - 1] = subs[subs.Length - 1].Substring(0, ioEquals);

            //Parse the elements
            Type = subs[1];
            UID = subs[2];
            Val = subs[3];
        }

        public override string FileExtension => ".world";
        public override string Filename => $"unique-{Type}-{UID}-{Val}.world";
        public override string Whereami => $"InstanceWorld:{Type}:{UID}:{Val}";
    }

    public partial class CelestialWorld : World
    {
        /// <summary>
        /// Universe X coordinate of the planet
        /// </summary>
        public long X { get; set; }

        /// <summary>
        /// Universe Y coordinate of the planet
        /// </summary>
        public long Y { get; set; }

        /// <summary>
        /// Universe Z coordinate of the planet
        /// </summary>
        public long Z { get; set; }

        /// <summary>
        /// The planet ID of the world
        /// </summary>
        public int Planet { get; set; }

        /// <summary>
        /// The moon ID of the world. Only set if the world is a moon.
        /// </summary>
        public int? Moon { get; set; }

        /// <summary>
        /// Is this world a moon?
        /// </summary>
        public bool IsMoon => Moon.HasValue;

        /// <summary>
        /// Creates a new instance of the world.
        /// </summary>
        public CelestialWorld() { }

        /// <summary>
        /// Creates a new instance of the world, parsing the subs from the whereami information.
        /// </summary>
        /// <param name="subs"></param>
        internal CelestialWorld(string[] subs)
        {
            //CelestialWorld:17:17:-198600707:10
            //CelestialWorld:-55603946:357637721:-294801013:10=4934.94.1173.41
            if (subs.Length < 5) throw new ArgumentOutOfRangeException("subs", subs.Length, "CelestialWorld subs constructor requires at least 5 elements");
            if (subs[0] != "CelestialWorld") throw new ArgumentException("The first element of the subs must be equal to CelestialWorld");

            //Remove the =4934.94.1173.41 of the last element
            int ioEquals = subs[subs.Length - 1].IndexOf('=');
            if (ioEquals >= 0) subs[subs.Length - 1] = subs[subs.Length - 1].Substring(0, ioEquals);

            //Parse the elements
            X = int.Parse(subs[1]);
            Y = int.Parse(subs[2]);
            Z = int.Parse(subs[3]);
            Planet = int.Parse(subs[4]);
            if (subs.Length == 6) Moon = int.Parse(subs[5]);
        }

        public override string FileExtension => ".world";
        public override string Filename => $"{X}_{Y}_{Z}_{Planet}{(Moon.HasValue ? $"_{Moon.Value}" : "")}.world";
        public override string Whereami => $"CelestialWorld:{X}:{Y}:{Z}:{Planet}" + (Moon.HasValue ? $":{Moon.Value}" : "");
    }
}
