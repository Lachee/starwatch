using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Starwatch.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Starwatch.Serializer
{
    public class WorldSerializer : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            //Get the list
            World world = value as World;
            if (world == null)
            {
                writer.WriteNull();
                return;
            }

            //WRite the value
            writer.WriteValue(world.Whereami);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            string whereami = reader.Value as string;
            if (whereami == null) return existingValue;
            return World.Parse(whereami);
        }

        public override bool CanRead { get { return true; } }
        public override bool CanConvert(Type objectType) => typeof(World).IsAssignableFrom(objectType);
    }
}
