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
using System.Timers;

namespace Starwatch.Util
{
    public static class Time
    {
        /// <summary>
        /// Converts the DateTime into a Unix Epoch
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static long ToUnixEpoch(this DateTime time) => (long)(time - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;

        /// <summary>
        /// Converts the long to a date time using Unix Epoch
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static DateTime ToDateTime(this long time) => new DateTime(1970, 1, 1, 0, 0, 0) + TimeSpan.FromSeconds(time);

        /// <summary>
        /// Resets the timer
        /// </summary>
        /// <param name="timer"></param>
        public static void Reset(this Timer timer)
        {
            if (timer.Enabled)
            {
                timer.Stop();
                timer.Start();
            }
        }
    }
}
