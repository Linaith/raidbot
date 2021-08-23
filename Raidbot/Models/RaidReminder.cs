namespace Raidbot.Models
{
    public class RaidReminder
    {
        public enum ReminderType
        {
            User = 0,
            Channel = 1
        }

        public RaidReminder(ReminderType type, string message, double hoursBeforeRaid, ulong channelId = 0)
        {
            Type = type;
            Message = message;
            HoursBeforeRaid = hoursBeforeRaid;
            ChannelId = channelId;
        }

        public ReminderType Type { get; set; }

        public string Message { get; set; }

        public double HoursBeforeRaid { get; set; }

        public ulong ChannelId { get; set; }

        public bool Sent { get; set; } = false;
    }
}
