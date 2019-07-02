using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

namespace Starwatch.Database
{
    public static class DbDataReaderExtensions
    {
        public static bool IsDBNull(this DbDataReader dbReader, string name) => dbReader.IsDBNull(dbReader.GetOrdinal(name));
        public static string GetString(this DbDataReader dbReader, string name) => dbReader.IsDBNull(name) ? null : dbReader.GetString(dbReader.GetOrdinal(name));
        public static short GetInt16(this DbDataReader dbReader, string name) => dbReader.GetInt16(dbReader.GetOrdinal(name));
        public static int GetInt32(this DbDataReader dbReader, string name) => dbReader.GetInt32(dbReader.GetOrdinal(name));
        public static long GetInt64(this DbDataReader dbReader, string name) => dbReader.GetInt64(dbReader.GetOrdinal(name));
        public static double GetDouble(this DbDataReader dbReader, string name) => dbReader.GetDouble(dbReader.GetOrdinal(name));
        public static float GetFloat(this DbDataReader dbReader, string name) => dbReader.GetFloat(dbReader.GetOrdinal(name));
        public static DateTime GetDateTime(this DbDataReader dbReader, string name) => dbReader.GetDateTime(dbReader.GetOrdinal(name));
        public static DateTime? GetDateTimeNullable(this DbDataReader dbReader, string name) => dbReader.IsDBNull(name) ? null : (DateTime?)dbReader.GetDateTime(dbReader.GetOrdinal(name));
        public static bool GetBoolean(this DbDataReader dbReader, string name) => dbReader.GetBoolean(dbReader.GetOrdinal(name));
    }
}
