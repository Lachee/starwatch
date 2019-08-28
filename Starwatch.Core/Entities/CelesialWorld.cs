using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Starwatch.Database;
using Starwatch.Starbound;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starwatch.Entities
{
    public partial class CelestialWorld
    {
        /// <summary>
        /// The details of the world. This will be null until <see cref="GetDetailsAsync"/> caches it.
        /// </summary>
        public Metadata Details { get; private set; }

        public class Metadata : IRecord
        {
            public string Table => "!worlds";

            /// <summary>
            /// The world we are associated with
            /// </summary>
            [JsonIgnore]
            public CelestialWorld World { get; private set; }

            /// <summary>
            /// The whereami of the world.
            /// </summary>
            [JsonIgnore]
            public string Whereami
            {
                get => World?.Whereami;
                set => World = Entities.World.Parse(value) as CelestialWorld;
            }

            public long Seed { get; set; }

            [JsonIgnore]
            public TaggedText Name { get; set; }        //The name. celestial/name

            [JsonProperty("Name")]
            public string NameTagged => Name?.TaggedContent;

            public string Description { get; set; } //Tier 3 Moon etc. celestial/parameters/description
            public string TerrainSize { get; set; }   //small, medium, etc. celestial/parameters/worldSize
            public string TerrainType { get; set; }   //Terrestrial. celestial/parameters/worldType
            public string PrimaryBiome { get; set; } //The primary biome, like savannah. world/primaryBiome

            //public int Planet { get; set; }         //The planet id. celestial/coordinate/planet
            //public int? Satellite { get; set; }     //This will be 0 if we are not a satellite. celestial/coordinate/satellite
            //public int[] System { get; set; }       //Array of 3 for the sys coords. celestial/coordinate/location

            /// <summary>
            /// Length of the day
            /// </summary>
            public float DayLength { get; set; }

            /// <summary>
            /// Threat Level of the world
            /// </summary>
            public int ThreatLevel { get; set; }

            /// <summary>
            /// Json Metadata on how the planet is rendered
            /// </summary>
            public JToken PlanetGraphics { get; set; }

            internal Metadata() { }
            internal Metadata(CelestialWorld world)
            {
                World = world;
            }

            internal Metadata(string whereami)
            {
                World = Entities.World.Parse(whereami) as CelestialWorld;
            }

            public void LoadJObject(JObject jobj)
            {
                Seed = jobj["seed"].Value<long>();
                Name = new TaggedText(jobj["celestial"]["name"].Value<string>());
                Description = jobj["celestial"]["parameters"]["description"].Value<string>();
                TerrainSize = jobj["celestial"]["parameters"]["worldSize"].Value<string>();
                TerrainType = jobj["celestial"]["parameters"]["worldType"].Value<string>();
                PrimaryBiome = jobj["world"]["primaryBiome"].Value<string>();
                PlanetGraphics = jobj["sky"]["planet"]; //.ToString(Formatting.None);
                DayLength = jobj["world"]["dayLength"].Value<float>();
                ThreatLevel = jobj["world"]["threatLevel"].Value<int>();

                //Planet = jobj["celestial"]["coordinate"]["planet"].Value<int>();
                //Satellite = jobj["celestial"]["coordinate"]["satellite"].Value<int>();
                //if (Satellite.Value < 1) Satellite = null;
                //
                //System = new int[]
                //{
                //    jobj["celestial"]["coordinate"]["location"][0].Value<int>(),
                //    jobj["celestial"]["coordinate"]["location"][1].Value<int>(),
                //    jobj["celestial"]["coordinate"]["location"][2].Value<int>()
                //};


            }

            /// <summary>
            /// Loads the details
            /// </summary>
            /// <param name="db"></param>
            /// <returns></returns>
            public async Task<bool> LoadAsync(DbContext db)
            {
                //TODO: Re-Enable
                var details = await db.SelectOneAsync<Metadata>(Table, LoadFromDbDataReader, new Dictionary<string, object>() { { "whereami", Whereami } });
                return details != default(Metadata);
            }

            /// <summary>
            /// Loads the data from the db data reader.
            /// </summary>
            /// <param name="reader"></param>
            /// <returns></returns>
            internal Metadata LoadFromDbDataReader(DbDataReader reader)
            {
                Seed = reader.GetInt64("seed");
                Name = new TaggedText(reader.GetString("name"));
                Description = reader.GetString("description");
                TerrainSize = reader.GetString("size");
                TerrainType = reader.GetString("type");
                PrimaryBiome = reader.GetString("biome");
                PlanetGraphics = JToken.Parse(reader.GetString("planet_graphics"));
                DayLength = reader.GetFloat("day_length");
                ThreatLevel = reader.GetInt32("threat_level");

                //I only really want to update the world if it doesnt already exist.
                if (World == null)
                    Whereami = reader.GetString("whereami");

                return this;
            }

            /// <summary>
            /// Saves the details
            /// </summary>
            /// <param name="db"></param>
            /// <returns></returns>
            public async Task<bool> SaveAsync(DbContext db)
            {
                long id = await db.InsertUpdateAsync(Table, new Dictionary<string, object>()
                {
                    ["whereami"]        = Whereami,
                    ["seed"]            = Seed,

                    ["x"]               = World.X,
                    ["y"]               = World.Y,
                    ["z"]               = World.Z,

                    ["name"]            = Name.TaggedContent,
                    ["name_clean"]      = Name.Content,
                    ["description"]     = Description,
                    ["size"]            = TerrainSize,
                    ["type"]            = TerrainType,
                    ["biome"]           = PrimaryBiome,
                    ["planet_graphics"] = PlanetGraphics.ToString(Formatting.None),
                    ["day_length"]      = DayLength,
                    ["threat_level"]    = ThreatLevel
                });

                return id > 0;
            }
        }

        public static Task<List<CelestialWorld>> SearchCoordinatesAsync(DbContext db, long? xMin, long? xMax, long? yMin, long? yMax)
        {
            Dictionary<string, object> arguments = new Dictionary<string, object>();
            List<string> conditions = new List<string>();

            if (xMin.HasValue)
            {
                conditions.Add("x > ?xMin");
                arguments.Add("xMin", xMin.Value);
            }
            
            if (xMax.HasValue)
            {
                conditions.Add("x < ?xMax");
                arguments.Add("xMax", xMax.Value);
            }
            
            if (yMin.HasValue)
            {
                conditions.Add("y > ?yMin");
                arguments.Add("yMin", yMin.Value);
            }
            
            if (yMax.HasValue)
            {
                conditions.Add("y < ?yMax");
                arguments.Add("yMax", yMax.Value);
            }


            string query = "SELECT * FROM !worlds";
            if (conditions.Count > 0) query += " WHERE " + string.Join(" AND ", conditions);
            return db.ExecuteAsync<CelestialWorld>(query, FromDbDataReader, arguments);
        }

        private static CelestialWorld FromDbDataReader(DbDataReader reader)
        {
            //Load from the data reader
            var metadata = new Metadata();
            metadata.LoadFromDbDataReader(reader);

            //get the world and update the details
            var world = metadata.World;
            world.Details = metadata;

            //return the world
            return world;
        }

        /// <summary>
        /// Reads the details from the json metadata
        /// </summary>
        /// <param name="server"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public async Task<Metadata> CreateDetailsAsync(Server server, DbContext db = null)
        {
            //Exports the file 
            string filepath = await this.ExportJsonDataAsync(server, false);
            string json = File.ReadAllText(filepath);
            var jobj = JObject.Parse(json);

            //Create the metadata and read from jobj
            Details = new Metadata(this);
            Details.LoadJObject(jobj);
            await Details.SaveAsync(db ?? server.DbContext);

            //Return the details
            return Details;
        }

        /// <summary>
        /// Loads the details. If it does not exist, it will try to create the details instead.
        /// </summary>
        /// <param name="server"></param>
        /// <param name="db"></param>
        /// <param name="save">Save the world to the database if it is created.</param>
        /// <returns></returns>
        public async Task<Metadata> GetDetailsAsync(Server server, DbContext db = null)
        {
            //Prepare the details
            Details = new Metadata(this);

            //Try to load it. If we fail, create and save new details
            if (!await Details.LoadAsync(db ?? server.DbContext))
            {
                try
                {
                    Details = await CreateDetailsAsync(server, db ?? server.DbContext);
                }
                catch (Exception)
                {
                    Details = null;
                }
            }

            //Return the details.
            return Details;
        }

        //public async Task<bool> SaveDetailsAsync(DbContext db )
        //{
        //    //We dont have anything, so abort
        //    if (Details == null)
        //        return false;
        //
        //    await Details.SaveAsync(db);
        //    return true;
        //}
    }
}
