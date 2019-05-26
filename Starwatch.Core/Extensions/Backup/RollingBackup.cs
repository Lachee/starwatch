namespace Starwatch.Extensions.Backup
{
    public class RollingBackup
    {
        public WorldBackup WorldBackup { get; }
        public int Interval { get; }
        public int Countdown { get; private set; }

        public string GetFilename() => WorldBackup.World.Filename + $".{Interval}.bak";

        public RollingBackup(WorldBackup worldBackup, int interval)
        {
            WorldBackup = worldBackup;
            Interval = interval;
            Countdown = interval;
        }

        /// <summary>
        /// Decrements the countdown, automatically reseting it if it reaches 0. Will return true if it resets.
        /// </summary>
        /// <param name="time">The time to decrement by</param>
        /// <returns></returns>
        public bool DecrementCountdown(int time)
        {
            Countdown -= time;
            if (Countdown <= 0)
            {
                Countdown = Interval;
                return true;
            }

            return false;
        }
    }
}
