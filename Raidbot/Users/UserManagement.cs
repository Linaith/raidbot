using Discord;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Raidbot.Users
{
    public static class UserManagement
    {
        private static readonly string _jsonFile = Path.Combine(Constants.SAVEFOLDER, "users.json");


        //guildId, Server
        private static Dictionary<ulong, DiscordServer> _discordServers;

        static UserManagement()
        {
            LoadUsers();
        }

        public static void AddAccountType(ulong guildId, string accountType)
        {
            if (!_discordServers.ContainsKey(guildId))
            {
                _discordServers.Add(guildId, new DiscordServer(guildId));
            }

            _discordServers[guildId].AddAccountType(accountType);
            SaveUsers();
        }

        public static bool RemoveAccountType(ulong guildId, string accountType)
        {
            if (!_discordServers.ContainsKey(guildId))
            {
                return false;
            }

            bool result = _discordServers[guildId].RemoveAccountType(accountType);
            SaveUsers();
            return result;
        }

        public static DiscordServer GetServer(ulong guildId)
        {
            if (!_discordServers.ContainsKey(guildId))
            {
                _discordServers.Add(guildId, new DiscordServer(guildId));
            }
            return _discordServers[guildId];
        }

        public static void SaveUsers()
        {
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(_discordServers);
            File.WriteAllText(_jsonFile, json);
        }

        private static void LoadUsers()
        {
            if (File.Exists(_jsonFile))
            {
                string json = File.ReadAllText(_jsonFile);
                _discordServers = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<ulong, DiscordServer>>(json);
            }
            else
            {
                _discordServers = new Dictionary<ulong, DiscordServer>();
            }
        }

        public static async Task UpdateNameAsync(IGuildUser user)
        {
            if (_discordServers.ContainsKey(user.GuildId) && _discordServers[user.GuildId].ChangeNames && _discordServers[user.GuildId].Users.ContainsKey(user.Id))
            {
                string nickname = _discordServers[user.GuildId].GetUser(user.Id).DiscordName;
                if (!string.IsNullOrEmpty(nickname))
                {
                    await user.ModifyAsync(p => p.Nickname = nickname);
                }
            }
        }
    }
}
