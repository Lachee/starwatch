using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

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

        /// <summary>
        /// Parses the whereami to a world object.
        /// </summary>
        /// <param name="whereami"></param>
        /// <returns></returns>
        private static World ParseWhereami(string whereami)
        {
            //Seperate the parts
            string[] subs = whereami.Split(':');
            if (subs.Length < 2 || subs.Length > 6) return null;

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
        private static World ParseFilename(string filename)
        {
            //Trim it a bit so its only the important data.
            filename = System.IO.Path.GetFileName(filename);

            //Check if its a instance world
            bool isInstance = filename.StartsWith("unique");

            //Some basic manipulation of the filename so we dont have to do as much assignments later on.
            //Specifically, we are going to add the first part so when we split it no futher work is required.
            if (isInstance)
                filename = filename.Replace("unique-", "InstanceWorld-");
            else
                filename = "CelestialWorld_" + filename;

            //Split the parts into subs
            string[] subs = filename.Split(isInstance ? '-' : '_');

            //Clear the .world part
            string ending = subs[subs.Length - 1];
            if (ending.EndsWith(".world"))
            {
                ending = ending.Substring(0, ending.Length - 6);
                subs[subs.Length - 1] = ending;
            }

            //return the correct object
            if (isInstance) return new InstanceWorld(subs);
            return new CelestialWorld(subs);
        }

        /// <summary>
        /// Parses the identifier too a World. The identifier can be either the result from a whereami or a filename ending with .world. Returns null if the pattern is not valid.
        /// </summary>
        /// <param name="identifier">The string identifier of the world, either the result of /whereami or the filename ending with .world</param>
        /// <returns></returns>
        public static World Parse(string identifier)
        {
            bool isFilename = identifier.EndsWith(".world") || !identifier.Contains(":");
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

        public override string Filename => $"unique-{Type}-{UID}-{Val}.world";
        public override string Whereami => $"InstanceWorld:{Type}:{UID}:{Val}";
    }

    public class CelestialWorld : World
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
        /// The system ID the world is in.
        /// </summary>
        public long System { get; set; }

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
            System = int.Parse(subs[3]);
            Planet = int.Parse(subs[4]);
            if (subs.Length == 6) Moon = int.Parse(subs[5]);
        }

        public override string Filename => $"{X}_{Y}_{System}_{Planet}{(Moon.HasValue ? $"_{Moon.Value}" : "")}.world";
        public override string Whereami => $"CelestialWorld:{X}:{Y}:{System}:{Planet}" + (Moon.HasValue ? $":{Moon.Value}" : "");
    }
}
