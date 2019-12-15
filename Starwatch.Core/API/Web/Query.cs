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
using System.Collections.Generic;
using System.Collections.Specialized;
using WebSocketSharp.Net;

namespace Starwatch.API.Web
{
    /// <summary>
    /// A collection of parameters from REST calls.
    /// </summary>
    public class Query : Dictionary<string, string>
    {
        public const string AsyncKey = "async";

        public bool IsAsync => GetBool(AsyncKey, false);

        public Query() : base(0) { }
        public Query(int capacity) : base(capacity) { }
        public Query(Dictionary<string, string> collection) : base(collection) { }
        public Query(HttpListenerRequest request) : this(request.QueryString) { }
        public Query(NameValueCollection collection) : base(collection.Count)
        {
            foreach (string key in collection.AllKeys)
            {
                if (key == null)
                {
                    //No key, so we must have some single properties
                    //Get all the single queries
                    string[] values = collection.GetValues(null);

                    //Add them as "true" in our collection
                    for (int i = 0; i < values.Length; i++)
                        this.Add(values[i], "true");
                }
                else
                {
                    //We have a normal keypair, so just add it.
                    this.Add(key, collection[key]);
                }
            }
        }


        /// <summary>
        /// Trys to get a int
        /// </summary>
        /// <param name="key">key of the query</param>
        /// <param name="result">Resulting value</param>
        /// <returns></returns>
        public bool TryGetInt(string key, out int result)
        {
            string v;
            if (!TryGetValue(key, out v)) { result = default(int); return false; }
            return int.TryParse(v, out result);
        }

        /// <summary>
        /// Trys to get a bool
        /// </summary>
        /// <param name="key">key of the query</param>
        /// <param name="result">Resulting value</param>
        /// <returns></returns>
        public bool TryGetBool(string key, out bool result)
        {
            string v;

            //Make sure we have the value
            if (!TryGetValue(key, out v))
            {
                result = default(bool);
                return false;
            }

            //If its a bool, give straight up
            if (bool.TryParse(v, out result))
                return true;

            //Try to parse it as a int
            if (int.TryParse(v, out var num))
            {
                result = num == 1;
                return true;
            }

            //Everything else failed.
            return false;
        }

        /// <summary>
        /// Trys to get a double
        /// </summary>
        /// <param name="key">key of the query</param>
        /// <param name="result">Resulting value</param>
        /// <returns></returns>
        public bool TryGetDouble(string key, out double result)
        {
            string v;
            if (!TryGetValue(key, out v)) { result = default(double); return false; }
            return double.TryParse(v, out result);
        }

        /// <summary>
        /// Trys to get a long
        /// </summary>
        /// <param name="key">key of the query</param>
        /// <param name="result">Resulting value</param>
        /// <returns></returns>
        public bool TryGetLong(string key, out long result)
        {
            string v;
            if (!TryGetValue(key, out v)) { result = default(long); return false; }
            return long.TryParse(v, out result);
        }


        /// <summary>
        /// Tries to get a string
        /// </summary>
        /// <param name="key">key of the query</param>
        /// <param name="result">Resulting value</param>
        /// <returns></returns>
        public bool TryGetString(string key, out string result)
        {
            return TryGetValue(key, out result);
        }

        /// <summary>
        /// Gets the boolean, returning default if it doesnt not exist.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="default"></param>
        /// <returns></returns>
        public bool GetBool(string key, bool @default)
        {
            bool b;
            if (TryGetBool(key, out b)) return b;
            return @default;
        }

        /// <summary>
        /// Gets the int, returning default if it doesnt not exist.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="default"></param>
        /// <returns></returns>
        public int GetInt(string key, int @default)
        {
            int v;
            if (TryGetInt(key, out v)) return v;
            return @default;
        }

        /// <summary>
        /// Gets the double, returning default if it doesnt not exist.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="default"></param>
        /// <returns></returns>
        public double GetDouble(string key, double @default)
        {
            double v;
            if (TryGetDouble(key, out v)) return v;
            return @default;
        }

        /// <summary>
        /// Gets the long, returning default if it doesnt not exist.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="default"></param>
        /// <returns></returns>
        public long GetLong(string key, long @default)
        {
            long v;
            if (TryGetLong(key, out v)) return v;
            return @default;
        }

        /// <summary>
        /// Gets the string, returning default if it doesnt not exist.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="default"></param>
        /// <returns></returns>
        public string GetString(string key, string @default)
        {
            string v;
            if (TryGetString(key, out v)) return v;
            return @default;
        }



        /// <summary>
        /// Gets a optional long
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public long? GetOptionalLong(string key)
        {
            if (TryGetLong(key, out var r)) return r;
            return null;
        }



        /// <summary>
        /// Checks if we have all the keys passed. Ignores if we have extra keys, as long as we have the ones we want.
        /// </summary>
        /// <param name="keys">Any number of keys that is required</param>
        /// <returns>True if all keys are available</returns>
        public bool Validate(params string[] keys)
        {
            //How can we possibly have enough keys?
            if (keys.Length > this.Count) return false;
            for (int i = 0; i < keys.Length; i++)
            {
                if (i > this.Count || !this.ContainsKey(keys[i]))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Validates strictly if we only have the keys passed. If we do not have all the keys, or if we have extra keys, this will return false.
        /// </summary>
        /// <param name="keys">The keys we only want</param>
        /// <returns>True if we only have those keys.</returns>
        public bool ValidateStrict(params string[] keys)
        {
            //We are not matching in length, no way will we have the keys
            if (keys.Length != this.Count) return false;

            List<string> remain = new List<string>(keys);
            foreach (string key in this.Keys)
            {
                //We are a key we where not expecting!
                if (!remain.Contains(key))
                    return false;

                //Remove it from the remaining keys to expect
                remain.Remove(key);
            }

            return remain.Count == 0;
        }
    }
}
