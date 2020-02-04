using Starwatch.Logging;
using Starwatch.Util;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Starwatch.Database
{
    public class DbContext
    {
        public ConnectionSettings Settings { get; }
        public bool IsConnected { get; private set; }
        public Logger Logger { get; }

        public DateTime LastStatementPreparedAt { get; private set; }

        //TODO: Make a pool of connections, one for each thread
        private MySqlConnection _connection;
        private MySqlCommand _command;
        private SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        //TODO: Fix the threading issues.
        private DropoutStack<PreviousQuery> _previousQueries = new DropoutStack<PreviousQuery>(10);
        private struct PreviousQuery
        {
            public string query;
            public int thread;
            public override string ToString() => $"Thread: {thread}, Query: {query}";
        }


        public DbContext(ConnectionSettings settings, Logger logger = null)
        {
            this.Logger = logger ?? new Logger("SQL");
            this.Settings = settings;
        }

        /// <summary>
        /// Creates a new database context
        /// </summary>
        /// <returns></returns>
        public DbContext SpawnChild()
        {
            return new DbContext(Settings, Logger.Child("CHILD"));
        }

        /// <summary>
        /// Does a rough encryption of a string.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public string Encrypt(string str) => str.Encrypt(Settings.Passphrase);

        /// <summary>
        /// Does a rough decryption of a string
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public string Decrypt(string str) => str.Decrypt(Settings.Passphrase);

        /// <summary>
        /// Executes a non-query statement
        /// </summary>
        /// <param name="query"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        public async Task<int> ExecuteNonQueryAsync(string query, Dictionary<string, object> arguments = null)
        {
            //Create the command
            var cmd = await CreateCommandAsync(query, arguments);
            try
            {
                if (cmd == null) return 0;

                //Get the reader
                return await cmd.ExecuteNonQueryAsync();
            }
            finally
            {
                //Release the command
                ReleaseCommand();
            }
        }

        /// <summary>
        /// Selects just one elmement from the query
        /// </summary>
        /// <param name="query"></param>
        /// <param name="action"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        public async Task<T> ExecuteOneAsync<T>(string query, Func<DbDataReader, T> callback, Dictionary<string, object> arguments = null)
        {
            //Create the command
            var cmd = await CreateCommandAsync(query, arguments);
            try
            {
                if (cmd == null) return default(T);

                //Get the reader
                var reader = await cmd.ExecuteReaderAsync();
                if (reader == null) return default(T);

                //Read the content
                if (!await reader.ReadAsync()) return default(T);
                try
                {
                    return callback.Invoke(reader);
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "Failed to invoke reader: {0}");
                    return default(T);
                }
            }
            finally
            {
                //Release the command
                ReleaseCommand();
            }
        }

        /// <summary>
        /// Selects all the elements in the query
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>
        /// <param name="callback"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>

        public async Task<List<T>> ExecuteAsync<T>(string query, Func<DbDataReader, T> callback, Dictionary<string, object> arguments = null)
        {
            //Create the command & prepare the list
            var results = new List<T>();
            var cmd = await CreateCommandAsync(query, arguments);
            try
            {
                //No command, abort early
                if (cmd == null) return results;

                //Get the reader
                var reader = await cmd.ExecuteReaderAsync();
                if (reader == null) return results;

                //Read the content
                while (await reader.ReadAsync())
                {
                    try
                    {
                        results.Add(callback.Invoke(reader));
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e, "Failed to invoke reader: {0}");
                    }
                }

                //return the list
                return results;
            }
            finally
            {
                //Release the command
                ReleaseCommand();
            }
        }

        /// <summary>
        /// Selects all the columns from the table
        /// </summary>
        /// <typeparam name="T">The type to read</typeparam>
        /// <param name="table">The table to read</param>
        /// <param name="callback">The callback to the data reader</param>
        /// <param name="condition">The condition of the selector</param>
        /// <param name="order">What to select from</param>
        /// <returns></returns>
        public async Task<T> SelectOneAsync<T>(string table, Func<DbDataReader, T> callback, Dictionary<string, object> condition = null, string order = null)
        {
            StringBuilder qb = new StringBuilder();
            qb.Append($"SELECT * FROM {table}");

            if (condition != null && condition.Count > 0)
                qb.Append(" WHERE ").Append(string.Join(" AND ", condition.Select(kp => kp.Key + "=?" + kp.Key)));

            if (order != null)
                qb.Append(" ORDER BY " + order);

            qb.Append(" LIMIT 1");

            //Create the command
            return await ExecuteOneAsync(qb.ToString(), callback, condition);
        }

        /// <summary>
        /// Selects all the columns from the table
        /// </summary>
        /// <typeparam name="T">The type to read</typeparam>
        /// <param name="table">The table to read</param>
        /// <param name="callback">The callback to the data reader</param>
        /// <param name="condition">The condition of the selector</param>
        /// <param name="order">What to select from</param>
        /// <param name="limit">The max number of elements to read</param>
        /// <returns></returns>
        public async Task<List<T>> SelectAsync<T>(string table, Func<DbDataReader, T> callback, Dictionary<string, object> condition = null, string order = null, int? limit = null)
        {
            StringBuilder qb = new StringBuilder();
            qb.Append($"SELECT * FROM {table}");

            if (condition != null && condition.Count > 0)
                qb.Append(" WHERE ").Append(string.Join(" AND ", condition.Select(kp => kp.Key + "=?" + kp.Key)));

            if (order != null)
                qb.Append(" ORDER BY " + order);

            if (limit.HasValue)
            {
                qb.Append(" LIMIT " + limit.Value);
                System.Diagnostics.Debug.Assert(limit.Value > 1, "SelectOne should probably be used instead.");
            }

            //Create the command & prepare the list
            return await ExecuteAsync<T>(qb.ToString(), callback, condition);
        }

        /// <summary>
        /// Updates the table
        /// </summary>
        /// <param name="table">The table to update</param>
        /// <param name="columns">The columns to update and their values</param>
        /// <param name="condition">The conditions for the update.</param>
        /// <param name="limit">The optional limit</param>
        /// <returns></returns>
        public async Task<int> UpdateAsync(string table, Dictionary<string, object> columns, Dictionary<string, object> condition = null, int? limit = null)
        {
            StringBuilder qb = new StringBuilder();
            qb.Append($"UPDATE TABLE {table} SET ");
            qb.Append(string.Join(",", columns.Select(kp => kp.Key + "=?" + kp.Key)));

            if (condition != null && condition.Count > 0)
                qb.Append(" WHERE ").Append(string.Join(" AND ", condition.Select(kp => kp.Key + "=?" + kp.Key)));

            if (limit.HasValue)
            {
                qb.Append(" LIMIT " + limit.Value);
                System.Diagnostics.Debug.Assert(limit.Value > 1, "SelectOne should probably be used instead.");
            }

            //Create the command & prepare the list
            var cmd = await CreateCommandAsync(qb.ToString(), columns.Union(condition));
            try
            {
                //No command, abort early
                if (cmd == null) return 0;

                //Get the reader
                return await cmd.ExecuteNonQueryAsync();
            }
            finally
            {
                //Release the command
                ReleaseCommand();
            }
        }

        /// <summary>
        /// Inserts or Updates the data asyncronously
        /// </summary>
        /// <param name="query"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        public async Task<long> InsertUpdateAsync(string table, Dictionary<string, object> columns)
        {
            StringBuilder qb = new StringBuilder();
            qb.Append("INSERT INTO ").Append(table).Append(" (").Append(string.Join(",", columns.Keys)).Append(") ");
            qb.Append("VALUES (").Append(string.Join(",", columns.Keys.Select(v => "?" + v))).Append(") ");
            qb.Append("ON DUPLICATE KEY UPDATE ").Append(string.Join(",", columns.Keys.Select(v => v + "=?" + v)));

            string query = qb.ToString();
            var cmd = await CreateCommandAsync(query, columns);

            try
            {
                if (cmd == null) return 0;
                await cmd.ExecuteNonQueryAsync();
                return cmd.LastInsertedId;
            }
            finally
            {
                ReleaseCommand();
            }
        }

        /// <summary>
        /// Deletes an item
        /// </summary>
        /// <param name="table"></param>
        /// <param name="condition"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public async Task<bool> DeleteAsync(string table, Dictionary<string, object> condition, int? limit = null)
        {
            StringBuilder qb = new StringBuilder();
            qb.Append($"DELETE FROM {table}");

            if (condition.Count > 0)
                qb.Append(" WHERE ").Append(string.Join(" AND ", condition.Select(kp => kp.Key + "=?" + kp.Key)));

            if (limit.HasValue)
                qb.Append(" LIMIT " + limit.Value);

            var cmd = await CreateCommandAsync(qb.ToString(), condition);
            if (cmd == null) return false;
            try
            {
                return await cmd.ExecuteNonQueryAsync() > 0;

            }
            finally
            {
                ReleaseCommand();
            }
        }

        /// <summary>
        /// Creates a command with arguments
        /// </summary>
        /// <param name="query"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        public async Task<MySqlCommand> CreateCommand(string query)
        {
            //Validate the query
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentNullException("query");

            //Try connect
            if (!await OpenAsync()) return null;

            //Wait for the semephore
            //Logger.Log("Creating Command, Waiting Semaphore.. {0}", _semaphore.CurrentCount);
            await _semaphore.WaitAsync();

            //Logger.Log("[+] {0}", query.Substring(0, Math.Min(query.Length, 50)));
            
            //Validate the command is empty
            if (_command != null)
                throw new Exception("Attempted to create a new command, but the previous one hasn't been released.");

            //Push the query
            _previousQueries.Push(new PreviousQuery()
            {
                query = query,
                thread = Thread.CurrentThread.ManagedThreadId
            });

            //Update the timer and prepare the query
            LastStatementPreparedAt = DateTime.Now;
            query = query.Replace("!", Settings.Prefix);
            _command = new MySqlCommand(query, _connection);
            if (_command == null)
            {
                ReleaseCommand();
                return null;
            }

            return _command;
        }

        /// <summary>
        /// Creates a command with named arguments
        /// </summary>
        /// <param name="query"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        public async Task<MySqlCommand> CreateCommandAsync(string query, IEnumerable<KeyValuePair<string, object>> arguments = null)
        {
            var cmd = await CreateCommand(query);
            if (cmd != null && arguments != null)
            {
                foreach (var kp in arguments)
                    cmd.Parameters.AddWithValue(kp.Key, kp.Value);
            }

            return cmd;
        }

        /// <summary>
        /// Required to release the command semephore
        /// </summary>
        public void ReleaseCommand()
        {
            if (_command != null)
            {
                _command.Dispose();
                _command = null;
            }

            _semaphore.Release();
            //Logger.Log("[-] released");
        }


        /// <summary>
        /// Opens the database
        /// </summary>
        /// <returns></returns>
        public async Task<bool> OpenAsync()
        {
            try
            {
                //Logger.Log("[c] Attempting Connection...");

                //Wait for our turn
                await _semaphore.WaitAsync();

                //Connection is not opened, so lets open it now.
                if (_connection == null)
                {
                    //Disable our isconnected
                    IsConnected = false;

                    //Create the connection
                    _connection = new MySqlConnection(this.Settings.ConnectionString);
                    _connection.StateChange += async (sender, args) =>
                    {
                        //We have closed or broken, so lets close us.
                        if (args.CurrentState == System.Data.ConnectionState.Closed || args.CurrentState == System.Data.ConnectionState.Broken)
                            await this.CloseAsync();
                    };
                }

                if (IsConnected) return true;
                await _connection.OpenAsync();

                IsConnected = true;
                return true;
            }
            catch (Exception e)
            {
                Logger.LogError(e, "SQL Open Exception. {0}");
                await CloseAsync();
                return false;
            }
            finally
            {
                //We are done, and dont need our turn anymore.
                //Logger.Log("[c] State Changed");
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Closes the database then disposes it.
        /// </summary>
        public async Task CloseAsync()
        {
            try
            {
                //Close the connection asyncronously.
                //Logger.Log("Closing SQL");
                await _connection.CloseAsync();
                IsConnected = false;
            }
            catch (Exception e)
            {
                //An error has occured while trying to close it.
                Logger.LogError(e, "SQL Close Exception.");
            }
            finally
            {
                //Finally dispose of the connection.
                DisposeConnection();
            }
        }

        /// <summary>
        /// Disposes the connection
        /// </summary>
        public void DisposeConnection()
        {
            Logger.Log("Disposing Connection...");
            if (_connection != null)
            {
                //We are still apparently connected, force close it if we can.
                // We don't care for errors because we will handle them later anyways.
                if (IsConnected)
                    try { _connection.Close(); } catch (Exception) { }
                
                //Dispose of the connection and set it to null.
                _connection?.Dispose();
                _connection = null;
            }

            //Set our flag
            IsConnected = false;
        }

        /// <summary>
        /// Disposes the connection
        /// </summary>
        public void Dispose() { DisposeConnection(); }

        /// <summary>
        /// Imports a SQL export from phpMyAdmin
        /// </summary>
        /// <param name="filepath"></param>
        /// <param name="replacePrefix"></param>
        /// <returns></returns>
        public async Task ImportSqlAsync(string filepath, bool replacePrefix = true)
        {
            if (!File.Exists(filepath))
                throw new FileNotFoundException("The file was not found", filepath);

            //Generate the big long statement
            string[] lines = File.ReadAllLines(filepath);
            StringBuilder builder = new StringBuilder();
            foreach (var line in lines)
            {
                if (line.StartsWith("--") || line.StartsWith("/*") || line.EndsWith("*/"))
                    continue;
                
                builder.Append(!replacePrefix ? line : line.Replace("k_", this.Settings.Prefix));
            }

            //Prepare the queries
            string[] queries = builder.ToString().Split(';');
            foreach(var q in queries)
            {
                if (string.IsNullOrWhiteSpace(q)) continue;

                try
                {
                    var cmd = await CreateCommand(q);
                    await cmd.ExecuteScalarAsync();
                }
                catch(Exception e)
                {
                    Logger.LogError(e, "Import Failure!");
                    break;
                }
                finally
                {
                    ReleaseCommand();
                }
            }
        }

    }
}
