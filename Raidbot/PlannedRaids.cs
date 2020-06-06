using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Raidbot
{
    public static class PlannedRaids
    {
        private static readonly string _jsonFile = Path.Combine(Constants.SAVEFOLDER, "plannedRaids.json");
        private static Dictionary<string, Raid> Raids { get; set; }
        private static readonly Random _random = new Random();
        private const int REMINDER_ITERVAL = 30;

        static PlannedRaids()
        {
            Raids = new Dictionary<string, Raid>();
            LoadRaids();
        }

        public static void AddRaid(Raid raid, ulong guildId, ulong channelId, ulong messageId)
        {
            raid.GuildId = guildId;
            raid.ChannelId = channelId;
            raid.MessageId = messageId;
            Raids.Add(raid.RaidId, raid);
            SaveRaids();
        }

        public static bool TryFindRaid(string raidId, out Raid raid)
        {
            if (Raids.ContainsKey(raidId))
            {
                raid = Raids[raidId];
                return true;
            }
            raid = null;
            return false;
        }

        public static bool TryFindRaid(ulong GuildId, ulong ChannelId, ulong MessageId, out Raid raid)
        {
            foreach (var r in Raids)
            {
                if (r.Value.GuildId.Equals(GuildId) && r.Value.ChannelId.Equals(ChannelId) && r.Value.MessageId.Equals(MessageId))
                {
                    raid = r.Value;
                    return true;
                }
            }
            raid = null;
            return false;
        }

        public static bool UpdateRaid(string raidId, Raid raid)
        {
            if (Raids.ContainsKey(raidId))
            {
                Raids[raidId] = raid;
                SaveRaids();
                return true;
            }
            return false;
        }

        public static async Task<bool> RemoveRaid(string raidId, SocketGuild guild)
        {
            if (TryFindRaid(raidId, out Raid raid) && raid.GuildId.Equals(guild.Id))
            {
                IUserMessage userMessage = (IUserMessage)await guild.GetTextChannel(raid.ChannelId).GetMessageAsync(raid.MessageId);
                await userMessage.DeleteAsync();
                Raids.Remove(raidId);
                SaveRaids();
                return true;
            }
            return false;
        }

        public static string CreateRaidId()
        {
            string raidId = _random.Next().ToString();
            if (Raids.ContainsKey(raidId))
            {
                return CreateRaidId();
            }
            return raidId;
        }

        private static void SaveRaids()
        {
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(Raids);
            File.WriteAllText(_jsonFile, json);
        }

        private static void LoadRaids()
        {
            if (File.Exists(_jsonFile))
            {
                string json = File.ReadAllText(_jsonFile);
                Raids = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Raid>>(json);
            }
        }

        public static void RemoveUserFromAllRaids(IUser user)
        {
            foreach (var raid in Raids)
            {
                raid.Value.RemoveUser(user);
            }
        }

        public async static void SendReminder(object sender, System.Timers.ElapsedEventArgs e)
        {
            TimeZoneInfo cet = CreateTimeZone();
            DateTime now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, cet);
            try
            {
                foreach (var raid in Raids)
                {
                    if (raid.Value.StartTime.CompareTo(now.AddMinutes(REMINDER_ITERVAL)) <= 0 && !raid.Value.ReminderSent)
                    {
                        string message = $"The raid \"{raid.Value.Title}\" starts in {REMINDER_ITERVAL} minutes.";
                        await HelperFunctions.Instance().SendMessageToEveryRaidMember(raid.Value, message);
                        raid.Value.ReminderSent = true;
                    }
                }
            }
            catch { }
        }

        public async static void ResetRaid(object sender, System.Timers.ElapsedEventArgs e)
        {
            TimeZoneInfo cet = CreateTimeZone();
            DateTime now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, cet);
            try
            {
                foreach (var raid in Raids.Values)
                {
                    if (raid.StartTime.AddHours(raid.RaidDuration + 1).CompareTo(now) <= 0)
                    {
                        if (raid.Weekly)
                        {
                            raid.Reset();
                            raid.StartTime = raid.StartTime.AddDays(7);
                            raid.MessageId = await HelperFunctions.Instance().RepostRaidMessage(raid);
                            SaveRaids();
                        }
                        else
                        {
                            await RemoveRaid(raid.RaidId, HelperFunctions.Instance().GetGuildById(raid.GuildId));
                        }
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
