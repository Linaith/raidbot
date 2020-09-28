using Discord.WebSocket;
using System;

namespace Raidbot.Services
{
    class TimerService
    {
        static System.Timers.Timer _t;
        private readonly RaidService _raidService;
        private readonly DiscordSocketClient _client;
        private const int REMINDER_INTERVAL = 30;

        public TimerService(RaidService raidService, DiscordSocketClient client)
        {
            _raidService = raidService;
            _client = client;
        }

        public void Start()
        {
            _t = new System.Timers.Timer
            {
                AutoReset = true,
                Interval = TimeSpan.FromMinutes(1).TotalMilliseconds
            };
            _t.Elapsed += new System.Timers.ElapsedEventHandler(SendReminder);
            _t.Elapsed += new System.Timers.ElapsedEventHandler(ResetRaid);
            _t.Start();
        }

        public async void SendReminder(object sender, System.Timers.ElapsedEventArgs e)
        {
            TimeZoneInfo cet = CreateTimeZone();
            DateTime now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, cet);
            try
            {
                foreach (var raid in _raidService.ListRaids())
                {
                    if (raid.StartTime.CompareTo(now.AddMinutes(REMINDER_INTERVAL)) <= 0 && !raid.ReminderSent)
                    {
                        string message = $"The raid \"{raid.Title}\" starts in {REMINDER_INTERVAL} minutes.";
                        await _raidService.SendMessageToEveryRaidMember(raid, message);
                        raid.ReminderSent = true;
                    }
                }
            }
            catch { }
        }

        public async void ResetRaid(object sender, System.Timers.ElapsedEventArgs e)
        {
            TimeZoneInfo cet = CreateTimeZone();
            DateTime now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, cet);
            try
            {
                foreach (var raid in _raidService.ListRaids())
                {
                    if (raid.StartTime.AddHours(raid.RaidDuration + 1).CompareTo(now) <= 0)
                    {
                        if (raid.Frequency > 0)
                        {
                            raid.Reset();
                            raid.StartTime = raid.StartTime.AddDays(raid.Frequency);
                            raid.MessageId = await _raidService.RepostRaidMessage(raid);
                        }
                        else
                        {
                            await _raidService.RemoveRaid(raid.RaidId, _client.GetGuild(raid.GuildId));
                        }
                        _raidService.SaveRaids();
                    }
                }
            }
            catch { }
        }

        private static TimeZoneInfo CreateTimeZone()
        {
            // Define transition times to/from CEST
            TimeZoneInfo.TransitionTime startTransition, endTransition;
            startTransition = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 2, 0, 0),
                                                                              3, 5, DayOfWeek.Sunday);
            endTransition = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 3, 0, 0),
                                                                            10, 5, DayOfWeek.Sunday);

            // Define adjustment rule
            TimeSpan delta = new TimeSpan(1, 0, 0);
            TimeZoneInfo.AdjustmentRule adjustment;
            adjustment = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(new DateTime(1999, 10, 1), DateTime.MaxValue.Date, delta, startTransition, endTransition);

            // Create array for adjustment rules
            TimeZoneInfo.AdjustmentRule[] adjustments = { adjustment };
            // Define other custom time zone arguments
            string displayName = "(GMT-01:00) Central European Time";
            string standardName = "German Time";
            string daylightName = "German Daylight Time";
            TimeSpan offset = new TimeSpan(1, 0, 0);
            return TimeZoneInfo.CreateCustomTimeZone(standardName, offset, displayName, standardName, daylightName, adjustments);
        }
    }
}
