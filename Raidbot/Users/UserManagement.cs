using Discord;
using Discord.WebSocket;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            if (_discordServers.ContainsKey(user.GuildId) && _discordServers[user.GuildId].Users.ContainsKey(user.Id))
            {
                string nickname = _discordServers[user.GuildId].GetUser(user.Id).DiscordName;
                if (!string.IsNullOrEmpty(nickname))
                {
                    await user.ModifyAsync(p => p.Nickname = nickname);
                }
            }
        }














        //discordId, User
        private static Dictionary<ulong, UserOld> _oldUsers;

        private static void RewriteSaveFile()
        {
            if (File.Exists(_jsonFile))
            {
                string json = File.ReadAllText(_jsonFile);
                _oldUsers = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<ulong, UserOld>>(json);
                Dictionary<ulong, User> newUsers = new Dictionary<ulong, User>();
                foreach (var oldUser in _oldUsers)
                {
                    User newUser = new User(oldUser.Value.Guilds.First());
                    newUser.Name = oldUser.Value.Name;
                    foreach (var account in oldUser.Value.GuildWars2Accounts)
                    {
                        if (string.IsNullOrEmpty(account.Value))
                        {
                            newUser.AddAccount("GuildWars2", account.Value, out _);
                        }
                        else
                        {
                            newUser.AddAccount("GuildWars2", account.Key, out string _);
                        }
                    }
                    newUser.SetMainAccount(oldUser.Value.MainAccount);
                    newUsers.Add(oldUser.Key, newUser);
                }

                string newJson = Newtonsoft.Json.JsonConvert.SerializeObject(newUsers);
                File.WriteAllText(_jsonFile, newJson);
            }
        }

        private class UserOld
        {
            //AccountName, ApiKey
            public Dictionary<string, string> GuildWars2Accounts { get; set; }

            public string MainAccount { get; set; }

            public string Name { get; set; } = string.Empty;

            public List<ulong> Guilds { get; set; }

            public UserOld()
            {
                GuildWars2Accounts = new Dictionary<string, string>();
                Guilds = new List<ulong>();
            }

            public UserOld(string accountName, string apiKey)
            {
                GuildWars2Accounts = new Dictionary<string, string>();
                GuildWars2Accounts.Add(accountName, apiKey);
                Guilds = new List<ulong>();
                MainAccount = accountName;
            }
        }
    }
}
