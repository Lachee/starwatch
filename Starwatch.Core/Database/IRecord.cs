using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Starwatch.Database
{
    public interface IRecord
    {
        /// <summary>
        /// Name of the database table
        /// </summary>
        [JsonIgnore]
        string Table { get; }

        /// <summary>
        /// Loads data from the database in to the object. Returns true if the load was successful.
        /// </summary>
        /// <param name="db"></param>
        /// <returns></returns>
        Task<bool> LoadAsync(DbContext db);

        /// <summary>
        /// Saves data from the object into the database. Returns true if the record was inserted.
        /// </summary>
        /// <param name="db"></param>
        /// <returns></returns>
        Task<bool> SaveAsync(DbContext db);
    }
}
