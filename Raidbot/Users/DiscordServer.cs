using System.Collections.Generic;

namespace Raidbot.Users
{
    public class DiscordServer
    {
        public List<string> UsableAccountTypes { get; set; }
        //UserId, User
        public Dictionary<ulong, User> Users { get; set; }

        public bool ChangeNames { get; set; } = false;

        public ulong GuildId { get; }

        public ulong LogChannelId { get; set; }

        public DiscordServer(ulong guildId)
        {
            UsableAccountTypes = new List<string>();
            Users = new Dictionary<ulong, User>();
            GuildId = guildId;
        }
    }
}
