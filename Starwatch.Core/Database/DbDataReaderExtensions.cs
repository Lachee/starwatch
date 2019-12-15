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
