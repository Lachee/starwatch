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
using System;

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
