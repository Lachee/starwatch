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
using Starwatch.Database;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Starwatch.Database;

namespace Starwatch.Entities
{
    public partial class SystemWorld
    {
        /// <summary>
        /// Searches for all the cached systems asyncronously.
        /// </summary>
        /// <returns></returns>
        public static async Task<HashSet<SystemWorld>> SearchCachedSystemsAsync(DbContext context)
        {
            var systems = await context.ExecuteAsync<SystemWorld>("SELECT min(z) as z, x, y FROM !worlds GROUP BY x, y", (reader) =>
            {
                var s = new SystemWorld();
                s.Z = reader.GetInt64("z");
                s.X = reader.GetInt64("x");
                s.Y = reader.GetInt64("y");
                return s;
            });

            return new HashSet<SystemWorld>(systems);
        }

        /// <summary>
        /// Searches for all the cached systems asyncronously within the bounds.
        /// </summary>
        /// <returns></returns>
        public static async Task<HashSet<SystemWorld>> SearchCachedSystemsAsync(DbContext context, long? xMin, long? xMax, long? yMin, long? yMax)
        {
            var arguments = new Dictionary<string, object>()
            {
                ["xmin"] = xMin.GetValueOrDefault(long.MinValue),
                ["xmax"] = xMax.GetValueOrDefault(long.MaxValue),
                ["ymin"] = yMin.GetValueOrDefault(long.MinValue),
                ["ymax"] = yMax.GetValueOrDefault(long.MaxValue),
            };

            var systems = await context.ExecuteAsync<SystemWorld>($"SELECT min(z) as z, x, y FROM !worlds x > :xmin AND x < :xmax AND y > :ymin AND y < :ymin GROUP BY x, y", (reader) =>
            {
                var s = new SystemWorld();
                s.Z = reader.GetInt64("z");
                s.X = reader.GetInt64("x");
                s.Y = reader.GetInt64("y");
                return s;
            }, arguments);

            return new HashSet<SystemWorld>(systems);
        }

        /// <summary>
        /// Gets all the celestial worlds that are mapped to this system. Alias of <see cref="CelestialWorld.SearchCoordinatesAsync(DbContext, SystemWorld)"/>.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<List<CelestialWorld>> GetCachedCelestialWorldsAsync(DbContext context) => await CelestialWorld.SearchCoordinatesAsync(context, this);
    }
}
