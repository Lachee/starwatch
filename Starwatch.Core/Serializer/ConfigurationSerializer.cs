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
using Newtonsoft.Json.Linq;
using Starwatch.Util;
using System;
using System.Collections.Generic;

namespace Starwatch.Serializer
{
    class ConfigurationSerializer : JsonConverter
    {

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var config = value as Configuration;
            if (config == null)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteStartObject();
            {
                //Write the objects
                foreach (var kp in config.Data)
                {
                    writer.WritePropertyName(kp.Key);
                    kp.Value.WriteTo(writer);
                }

                //Write the children
                if (config._children != null) 
                    {
                    writer.WritePropertyName("children");
                    writer.WriteStartObject();
                    {
                        foreach(var kp in config._children)
                        {
                            writer.WritePropertyName(kp.Key);
                            serializer.Serialize(writer, kp.Value);
                        }
                    }
                    writer.WriteEndObject();
                }
            }
            writer.WriteEndObject();
            
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            //Prepare the child
            Dictionary<string, Configuration> children = null;

            //Read the values
            var baseObject = JObject.Load(reader);
            var childToken = baseObject.GetValue("children");
            
            //If we have children, remove the field from the base and then parse the children array
            if (childToken != null)
            {
                baseObject.Remove("children");
                children = childToken.ToObject<Dictionary<string, Configuration>>(serializer);
            }

            //Return a new configuration with the children and base object
            var config = new Configuration(baseObject) { _children = children };
            return config;
        }

        public override bool CanRead => true;
        public override bool CanWrite => true;
        public override bool CanConvert(Type objectType) => objectType == typeof(Configuration);
        
    }
}

/*
 * 
            while(reader.Read())
            {
                if (reader.TokenType == JsonToken.PropertyName)
                {
                    nextPropertyName = reader.Value.ToString();
                    continue;
                }
                
                if (reader.TokenType == JsonToken.StartObject || reader.TokenType == JsonToken.EndObject)
                {
                    continue;
                }

                if (nextPropertyName == "children")
                {
                    var children = serializer.Deserialize<Dictionary<string, Configuration>>(reader);
                    config._children = children;
                }
                else
                {
                    var obj = serializer.Deserialize(reader);
                    config.SetKey(nextPropertyName, obj);
                }
            
            }
*/