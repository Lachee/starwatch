using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Starwatch.Serializer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Starwatch.Util
{
    /// <summary>
    /// Handles configuration for plugins, server and starwatch itself. It reads JSON configuration data and provides methods and utilities to load, save, read and write configuration data. 
    /// </summary>
    [JsonObject(ItemConverterType = typeof(ConfigurationSerializer))]
    public class Configuration
    {
        /// <summary>
        /// The name of the configuration
        /// </summary>
        //[JsonProperty("name", Order = 0)]
        private string Name { get; set; }

        /// <summary>
        /// The filename the configuration is currently saved to. It is convension to suffix with the extension .config.
        /// </summary>
        public string Filename { get; set; }

        /// <summary>
        /// The raw JSON serialized data. If this is edited, <see cref="MarkDirty"/> needs to be called to update its state.
        /// </summary>
        public JObject Data { get; set; }
               
        /// <summary>
        /// The parent configuration
        /// </summary>
        public Configuration Parent { get; private set; }
        
        public Dictionary<string, Configuration> _children = null;
        
        /// <summary>
        /// Creates a new Configuration from the deseralized JSON data. To read from a file, use <see cref="FromFile(string)"/>.
        /// </summary>
        /// <param name="data">The data read from JSON deseralizer.</param>
        public Configuration(JObject data) { this.Data = data; }

        private Configuration(Configuration parent, string name)
        {
            this.Parent = parent;
            this.Name = name;
            Data = new JObject();
        }

        #region Try Gets
        /// <summary>
        /// Attempts to get a integer from the configuration
        /// </summary>
        /// <param name="key">Key to look for</param>
        /// <param name="value">Value to set</param>
        /// <returns>True if successfully found and parsed the integer configuration</returns>
        public bool TryGetInt(string key, out int value)
        {
            try
            {
                value = GetInt(key);
                return true;
            }
            catch
            {
                value = 0;
                return false;
            }
        }

        /// <summary>
        /// Attempts to get a float from the configuration
        /// </summary>
        /// <param name="key">Key to look for</param>
        /// <param name="value">Value to set</param>
        /// <returns>True if successfully found and parsed the float configuration</returns>
        public bool TryGetFloat(string key, out float value)
        {
            value = -1;

            try
            {
                value = GetFloat(key);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Attempts to get a double from the configuration
        /// </summary>
        /// <param name="key">Key to look for</param>
        /// <param name="value">Value to set</param>
        /// <returns>True if successfully found and parsed the double configuration</returns>
        public bool TryGetDouble(string key, out double value)
        {
            value = -1;

            try
            {
                value = GetDouble(key);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Attempts to get a long from the configuration
        /// </summary>
        /// <param name="key">Key to look for</param>
        /// <param name="value">Value to set</param>
        /// <returns>True if successfully found and parsed the long configuration</returns>
        public bool TryGetLong(string key, out long value)
        {
            value = -1;

            try
            {
                value = GetLong(key);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Attempts to get a boolean from the configuration
        /// </summary>
        /// <param name="key">Key to look for</param>
        /// <param name="value">Value to set</param>
        /// <returns>True if successfully found and parsed the boolean configuration</returns>
        public bool TryGetBool(string key, out bool value)
        {
            value = false;

            try
            {
                value = GetBool(key);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Attempts to get a string from the configuration
        /// </summary>
        /// <param name="key">Key to look for</param>
        /// <param name="value">Value to set</param>
        /// <returns>True if successfully found and parsed the string configuration</returns>
        public bool TryGetString(string key, out string value)
        {
            value = null;

            try
            {
                value = GetString(key);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Attempts to get a object from the configuration
        /// </summary>
        /// <param name="key">Key to look for</param>
        /// <param name="obj">Value to set</param>
        /// <returns>True if successfully found and parsed the object configuration</returns>
        public bool TryGetObject<T>(string key, out T obj)
        {
            JToken token;
            if (!Data.TryGetValue(key, out token))
            {
                obj = default(T);
                return false;
            }

            try
            {
                obj = token.ToObject<T>();
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                obj = default(T);
                return false;
            }

            return true;
        }
        #endregion

        #region Gets
        /// <summary>
        /// Try to get a integer. Will throw exceptions if the value does not exist or is not a integer.
        /// </summary>
        /// <param name="key">Key of the integer</param>
        /// <returns>Returns the value stored in the configuration</returns>
        public int GetInt(string key)
        {
            try
            {
                return Data.GetValue(key).ToObject<int>();
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        /// <summary>
        /// Try to get a float. Will throw exceptions if the value does not exist or is not a float.
        /// </summary>
        /// <param name="key">Key of the float</param>
        /// <returns>Returns the value stored in the configuration</returns>
        public float GetFloat(string key)
        {
            try
            {
                return Data.GetValue(key).ToObject<float>();
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        /// <summary>
        /// Try to get a double. Will throw exceptions if the value does not exist or is not a double.
        /// </summary>
        /// <param name="key">Key of the double</param>
        /// <returns>Returns the value stored in the configuration</returns>
        public double GetDouble(string key)
        {
            try
            {
                return Data.GetValue(key).ToObject<double>();
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        /// <summary>
        /// Try to get a long. Will throw exceptions if the value does not exist or is not a long.
        /// </summary>
        /// <param name="key">Key of the long</param>
        /// <returns>Returns the value stored in the configuration</returns>
        public long GetLong(string key)
        {
            try
            {
                return Data.GetValue(key).ToObject<long>();
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        /// <summary>
        /// Try to get a boolean. Will throw exceptions if the value does not exist or is not a boolean.
        /// </summary>
        /// <param name="key">Key of the boolean</param>
        /// <returns>Returns the value stored in the configuration</returns>
        public bool GetBool(string key)
        {
            try
            {
                return Data.GetValue(key).ToObject<bool>();
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// Try to get a string. Will throw exceptions if the value does not exist or is not a string.
        /// </summary>
        /// <param name="key">Key of the string</param>
        /// <returns>Returns the value stored in the configuration</returns>
        public string GetString(string key)
        {
            try
            {
                return Data.GetValue(key).ToObject<string>();
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// Try to get a object. Will throw exceptions if the value does not exist or is not a obejct.
        /// </summary>
        /// <param name="key">Key of the object</param>
        /// <returns>Returns the value stored in the configuration</returns>
        /// <seealso cref="GetObject{T}(string, T)"/>
        [System.Obsolete("This method has been made obsolete by GetObject<T>(string key); Please use that instead.")]
        public object GetObject(string key)
        {
            return GetObject<object>(key);
        }
        /// <summary>
        /// Try to get a object. Will throw exceptions if the value does not exist or is not a obejct.
        /// </summary>
        /// <param name="key">Key of the object</param>
        /// <returns>Returns the value stored in the configuration</returns>
        public T GetObject<T>(string key)
        {
            try
            {
                return Data.GetValue(key).ToObject<T>();
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        /// <summary>
        /// Gets an object from the configuration. If it is not found, the default is set and returned.
        /// </summary>
        /// <param name="key">Key of the object</param>
        /// <param name="def">The default value to set and return if the key was not found.</param>
        /// <returns>Returns the value stored in the configuration, otherwise the default if not found.</returns>
        public T GetObject<T>(string key, T def)
        {
            T value;
            if (!TryGetObject<T>(key, out value))
            {
                SetKey(key, def);
                return def;
            }

            return value;
        }

        /// <summary>
        /// Gets the sub configuration. Will create one if it does not already exist.
        /// </summary>
        /// <param name="key">Key of the sub-configuration</param>
        /// <returns>Returns the value stored in the configuration</returns>
        public Configuration GetConfiguration(string key)
        {
            if (_children == null) _children = new Dictionary<string, Configuration>();
            if (!_children.ContainsKey(key))
            {
                _children.Add(key, new Configuration(this, key));
            }
            else
            {
                _children[key].Parent = this;
                _children[key].Name = key;
            }

            return _children[key];
        }
        #endregion    #region Gets

        #region Defaults

        /// <summary>
        /// Gets an integer from the configuration. If it is not found, the default is set and returned.
        /// </summary>
        /// <param name="key">Key of the integer</param>
        /// <param name="def">The default value to set and return if the key was not found.</param>
        /// <returns>Returns the value stored in the configuration, otherwise the default if not found.</returns>
        public int GetInt(string key, int def)
        {
            int value;
            if (!TryGetInt(key, out value))
            {
                SetKey(key, def);
                return def;
            }

            return value;
        }
        /// <summary>
        /// Gets an float from the configuration. If it is not found, the default is set and returned.
        /// </summary>
        /// <param name="key">Key of the float</param>
        /// <param name="def">The default value to set and return if the key was not found.</param>
        /// <returns>Returns the value stored in the configuration, otherwise the default if not found.</returns>
        public float GetFloat(string key, float def)
        {
            float value;
            if (!TryGetFloat(key, out value))
            {
                SetKey(key, def);
                return def;
            }

            return value;
        }
        /// <summary>
        /// Gets an double from the configuration. If it is not found, the default is set and returned.
        /// </summary>
        /// <param name="key">Key of the double</param>
        /// <param name="def">The default value to set and return if the key was not found.</param>
        /// <returns>Returns the value stored in the configuration, otherwise the default if not found.</returns>
        public double GetDouble(string key, double def)
        {
            double value;
            if (!TryGetDouble(key, out value))
            {
                SetKey(key, def);
                return def;
            }

            return value;
        }
        /// <summary>
        /// Gets an long from the configuration. If it is not found, the default is set and returned.
        /// </summary>
        /// <param name="key">Key of the long</param>
        /// <param name="def">The default value to set and return if the key was not found.</param>
        /// <returns>Returns the value stored in the configuration, otherwise the default if not found.</returns>
        public long GetLong(string key, long def)
        {
            long value;
            if (!TryGetLong(key, out value))
            {
                SetKey(key, def);
                return def;
            }

            return value;
        }
        /// <summary>
        /// Gets an boolean from the configuration. If it is not found, the default is set and returned.
        /// </summary>
        /// <param name="key">Key of the boolean</param>
        /// <param name="def">The default value to set and return if the key was not found.</param>
        /// <returns>Returns the value stored in the configuration, otherwise the default if not found.</returns>
        public bool GetBool(string key, bool def)
        {
            bool value;
            if (!TryGetBool(key, out value))
            {
                SetKey(key, def);
                return def;
            }

            return value;
        }
        /// <summary>
        /// Gets an string from the configuration. If it is not found, the default is set and returned.
        /// </summary>
        /// <param name="key">Key of the string</param>
        /// <param name="def">The default value to set and return if the key was not found.</param>
        /// <returns>Returns the value stored in the configuration, otherwise the default if not found.</returns>
        public string GetString(string key, string def)
        {
            string value;
            if (!TryGetString(key, out value))
            {
                SetKey(key, def);
                return def;
            }

            return value;
        }
        #endregion

        #region Setters
        /*
        public void SetInt(string key, int value) { SetObject(key, value); }
        public void SetFloat(string key, float value) { SetObject(key, value); }
        public void SetDouble(string key, double value) { SetObject(key, value); }
        public void SetLong(string key, long value) { SetObject(key, value); }
        public void SetBool(string key, bool value) { SetObject(key, value); }
        public void SetString(string key, string value) { }
        */

        /// <summary>
		/// Sets the specified key to the value
        /// </summary>
        /// <param name="key">The key to set</param>
        /// <param name="value">The value to set the key too</param>
        /// <param name="save">If true, the configuration will save once the key is set. By default this is false.</param>
        public object SetKey(string key, object value, bool save = false)
        {
            if (value == null) return null;
            Data[key] = JToken.FromObject(value);
            if (save) Save();
            return Data[key];
        }

        /// <summary>
        /// Sets the specified key to the value only if it differs from what is already stored. Returns true if the key was set.
        /// </summary>
        /// <typeparam name="T">The type of the object we are setting</typeparam>
        /// <param name="key">The key</param>
        /// <param name="value">The value we wish to set the key too</param>
        /// <returns>True if the key was set.</returns>
        public bool TrySetKey<T>(string key, T value)
        {
            T stored;
            if (!TryGetObject<T>(key, out stored) || !stored.Equals(value))
            {
                SetKey(key, value);
                return true;
            }

            return false;
        }
        #endregion

        #region Helpers

        /// <summary>
        /// Removes a key from the config.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool RemoveKey(string name)
        {
            if (!HasKey(name)) return false;
            return Data.Remove(name);
        }

        /// <summary>
        /// Checks to see if configuration has key by attempting to access it.
        /// </summary>
        /// <param name="name">Name of the key</param>
        /// <returns>True if the key was successfully accessed.</returns>
        public bool HasKey(string name)
        {
            JToken tmp;
            return Data.TryGetValue(name, out tmp);
        }
        
        /// <summary>
        /// Returns a list of keys in the configuration
        /// </summary>
        /// <returns></returns>
        public string[] GetKeys()
        {
            List<string> keys = Data.Properties().Select(p => p.Name).ToList();
            return keys.ToArray();
        }

        /// <summary>
        /// Saves the config data to a file, ignoring the parent.
        /// </summary>
        /// <param name="file">Location to save the JSON file</param>
        /// <param name="forcesave">Force the configuration to be saved, even if its not dirty.</param>
        /// <param name="pretty">Should it be indented and made human readable?</param>   
        /// <exception cref="ArgumentException">Thrown when path length is 0.</exception>
        /// <exception cref="ArgumentNullException">Thrown when path is null</exception>
        /// <exception cref="PathTooLongException">Thrown when path is too long. Must be less than 248chars</exception>
        /// <exception cref="DirectoryNotFoundException">Thrown when specific path is invalid</exception>
        /// <exception cref="IOException">Thrown when a I/O error occured</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown when path is readonly</exception>
        /// <exception cref="FileNotFoundException">Thrown when the file is not found</exception>
        /// <exception cref="NotSupportedException">Thrown when path is a invalid format</exception>
        /// <exception cref="System.Security.SecurityException">Thrown when user does not have permissions</exception>
        public void Save(string file, bool pretty = true)
        {
            //Havn't got a filename
            if (string.IsNullOrEmpty(Filename))
                throw new ArgumentNullException("Cannot save a configuration that hasn't got a filename yet!");

            //Save the json
            string json = ToJson(pretty ? Formatting.Indented : Formatting.None);
            File.WriteAllText(file, json);
        }

        /// <summary>
        /// Converts the configuration into its JSON representation.
        /// </summary>
        /// <param name="formatting"></param>
        /// <returns></returns>
        public string ToJson(Formatting formatting)
        {
            return JsonConvert.SerializeObject(this, formatting, new ConfigurationSerializer());
        }

        /// <summary>
        /// Converts the JSON into a Configuration.
        /// </summary>
        /// <returns></returns>
        public static Configuration FromJson(string json)
        {
            return JsonConvert.DeserializeObject<Configuration>(json, new ConfigurationSerializer());
        }

        /// <summary>
        /// Saves the config data. If not parent, it will save to the last file.
        /// </summary>
        /// <param name="forcesave">Force the configuration to be saved, even if its not dirty.</param>
        /// <param name="pretty">Should it be indented and made human readable?</param>   
        /// <exception cref="ArgumentException">Thrown when path length is 0.</exception>
        /// <exception cref="ArgumentNullException">Thrown when there is no filename yet</exception>
        /// <exception cref="PathTooLongException">Thrown when path is too long. Must be less than 248chars</exception>
        /// <exception cref="DirectoryNotFoundException">Thrown when specific path is invalid</exception>
        /// <exception cref="IOException">Thrown when a I/O error occured</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown when path is readonly</exception>
        /// <exception cref="FileNotFoundException">Thrown when the file is not found</exception>
        /// <exception cref="NotSupportedException">Thrown when path is a invalid format</exception>
        /// <exception cref="System.Security.SecurityException">Thrown when user does not have permissions</exception>
        public void Save(bool pretty = true)
        {
            if (Parent == null)
            {
                try
                {
                    Save(Filename, pretty);
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
            else
            {
                Parent.Save(pretty);
            }
            
        }

        #endregion

        /// <summary>
        /// Creates a new configuraiton from a file.
        /// </summary>
        /// <param name="file">Valid JSON file to load</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">Thrown when path length is 0.</exception>
        /// <exception cref="ArgumentNullException">Thrown when path is null</exception>
        /// <exception cref="PathTooLongException">Thrown when path is too long. Must be less than 248chars</exception>
        /// <exception cref="DirectoryNotFoundException">Thrown when specific path is invalid</exception>
        /// <exception cref="IOException">Thrown when a I/O error occured</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown when path is readonly</exception>
        /// <exception cref="FileNotFoundException">Thrown when the file is not found</exception>
        /// <exception cref="NotSupportedException">Thrown when path is a invalid format</exception>
        /// <exception cref="System.Security.SecurityException">Thrown when user does not have permissions</exception>
        /// <exception cref="JsonSerializationException">Thrown when a invalid JSON file was passed</exception>
        public static Configuration FromFile(string file)
        {
            //If the file doesn't exist, just return the empty config
            if (!File.Exists(file))
                return new Configuration(new JObject()) { Filename = file, Name = Path.GetFileNameWithoutExtension(file) };

            try
            {
                //Load the config file and return it
                var config = FromJson(File.ReadAllText(file));
                if (config == null) return new Configuration(new JObject()) { Filename = file, Name = Path.GetFileNameWithoutExtension(file) };
                if (config.Data == null) config.Data = new JObject();
                config.Filename = file;
                return config;
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }
}
