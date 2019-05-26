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
