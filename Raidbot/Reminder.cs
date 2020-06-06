using System;

namespace Raidbot
{
    public static class Reminder
    {
        static System.Timers.Timer _t;

        public static void Start()
        {
            _t = new System.Timers.Timer
            {
                AutoReset = true,
                Interval = TimeSpan.FromMinutes(1).TotalMilliseconds
            };
            _t.Elapsed += new System.Timers.ElapsedEventHandler(PlannedRaids.SendReminder);
            _t.Elapsed += new System.Timers.ElapsedEventHandler(PlannedRaids.ResetRaid);
            _t.Start();
        }
    }
}
