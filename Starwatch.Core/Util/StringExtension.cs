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

namespace Starwatch.Util
{
    public static class StringExtension
    {
        /// <summary>
        /// Cuts a segment of the string
        /// </summary>
        /// <param name="str"></param>
        /// <param name="start">The starting index</param>
        /// <param name="end">The ending index</param>
        /// <returns></returns>
        public static string Cut(this string str, int start, int end)
        {
            if (end > str.Length) throw new ArgumentOutOfRangeException("end", "The end cannot be greater than the length of the string");
            if (start > end) throw new ArgumentOutOfRangeException("start", "The start cannot be greater than the end");
            if (start < 0) throw new ArgumentOutOfRangeException("start", "The start cannot be less than 0");
            return str.Substring(start, end - start);
        }

        /// <summary>
        /// Formats the timespan into the form of (Time) (Unit)[s].
        /// <para>TimeSpan of 0:03:59 will be 3 Minutes</para>
        /// <para>TimeSpan of 0:01:43 will be 1 Minute</para>
        /// <para>TimeSpan of 0:00:14 will be 14 Seconds</para>
        /// </summary>
        /// <param name="timespan"></param>
        /// <returns></returns>
        public static string Format(this TimeSpan timespan)
        {
            if (timespan.TotalDays >= 1) return Math.Round(timespan.TotalDays) + " Day" + (timespan.TotalDays != 1 ? "s" : "");
            if (timespan.TotalHours >= 1) return Math.Round(timespan.TotalHours) + " Hour" + (timespan.TotalHours != 1 ? "s" : "");
            if (timespan.TotalMinutes >= 1) return Math.Round(timespan.TotalMinutes) + " Minute" + (timespan.TotalMinutes != 1 ? "s" : "");
            if (timespan.TotalSeconds >= 1) return Math.Round(timespan.TotalSeconds) + " Second" + (timespan.TotalSeconds != 1 ? "s" : "");
            return Math.Round(timespan.TotalMilliseconds) + "ms";
        }
    }
}
