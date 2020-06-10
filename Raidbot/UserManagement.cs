using Discord;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Raidbot
{
    public static class UserManagement
    {
        private static readonly string _jsonFile = Path.Combine(Constants.SAVEFOLDER, "users.json");

        //discordId, User
        private static Dictionary<ulong, User> _users;

        static UserManagement()
        {
            LoadUsers();
        }

        public static async Task AddGuildwars2ApiKey(ulong userId, string apiKey, ulong? guildId = null)
        {
            var connection = new Gw2Sharp.Connection(apiKey);
            using var client = new Gw2Sharp.Gw2Client(connection);
            var webApiClient = client.WebApi.V2;

            try
            {
                var account = await webApiClient.Account.GetAsync();
                if (!_users.ContainsKey(userId))
                {
                    _users.Add(userId, new User(account.Name, apiKey));
                    if (guildId != null)
                    {
                        _users[userId].Guilds.Add((ulong)guildId);
                    }
                    await ChangeMainAccount(userId, account.Name);
                }
                else if (_users[userId].GuildWars2Accounts.ContainsKey(account.Name))
                {
                    _users[userId].GuildWars2Accounts[account.Name] = apiKey;
                }
                else
                {
                    _users[userId].GuildWars2Accounts.Add(account.Name, apiKey);
                    if (string.IsNullOrEmpty(_users[userId].MainAccount))
                    {
                        await ChangeMainAccount(userId, account.Name);
                    }
                }
                SaveUsers();
            }
            catch
            {
                //TODO: exception handling?
            }
        }

        public static async Task<bool> AddGuildwars2Account(ulong userId, string accountName, ulong? guildId = null)
        {
            try
            {
                if (!Regex.IsMatch(accountName, Constants.ACCOUNT_REGEX))
                {
                    return false;
                }
                if (!_users.ContainsKey(userId))
                {
                    _users.Add(userId, new User(accountName, string.Empty));
                    if (guildId != null)
                    {
                        _users[userId].Guilds.Add((ulong)guildId);
                    }
                    await ChangeMainAccount(userId, accountName);
                }
                else if (!_users[userId].GuildWars2Accounts.ContainsKey(accountName))
                {
                    _users[userId].GuildWars2Accounts.Add(accountName, string.Empty);
                    if (string.IsNullOrEmpty(_users[userId].MainAccount))
                    {
                        await ChangeMainAccount(userId, accountName);
                    }
                }
                SaveUsers();
            }
            catch
            {
                //TODO: exception handling?
            }
            return true;
        }

        public static async Task<bool> RemoveGuildWars2Account(ulong userId, string accountName)
        {
            if (_users.ContainsKey(userId))
            {
                bool result = _users[userId].GuildWars2Accounts.Remove(accountName);
                if (_users[userId].MainAccount == accountName && result)
                {
                    if (_users[userId].GuildWars2Accounts.Count > 0)
                    {
                        await ChangeMainAccount(userId, _users[userId].GuildWars2Accounts.First().Key);
                    }
                    else
                    {
                        await ChangeMainAccount(userId, string.Empty);
                    }
                    await ChangeNickname(userId);
                }
                SaveUsers();
                return result;
            }
            return false;
        }

        public static async Task<bool> ChangeMainAccount(ulong userId, string accountName)
        {
            if (_users.ContainsKey(userId) && _users[userId].GuildWars2Accounts.ContainsKey(accountName))
            {
                _users[userId].MainAccount = accountName;
                SaveUsers();
                await ChangeNickname(userId);
                return true;
            }
            return false;
        }

        public static void AddServer(IGuildUser user)
        {
            if (_users.ContainsKey(user.Id) && !_users[user.Id].Guilds.Contains(user.Guild.Id))
            {
                _users[user.Id].Guilds.Add(user.Guild.Id);
            }
        }

        public static async Task UpdateNameAsync(IGuildUser user)
        {
            if (_users.ContainsKey(user.Id))
            {
                await ChangeNickname(user);
            }
        }

        public static async Task UpdateAllNamesAsync(IGuild guild)
        {
            foreach (IGuildUser user in await guild.GetUsersAsync())
            {
                AddServer(user);
                await UpdateNameAsync(user);
            }
        }

        public static string GetAccountByIndex(ulong userId, int index)
        {
            if (_users.ContainsKey(userId))
            {
                int i = 1;
                foreach (string account in _users[userId].GuildWars2Accounts.Keys)
                {
                    if (i == index) return account;
                    i++;
                }
            }
            return string.Empty;
        }

        public static IEnumerable<string> GetGuildWars2AccountNames(ulong userId)
        {
            if (_users.ContainsKey(userId))
            {
                return _users[userId].GuildWars2Accounts.Keys;
            }
            return new List<string>();
        }

        private static void SaveUsers()
        {
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(_users);
            File.WriteAllText(_jsonFile, json);
        }

        private static void LoadUsers()
        {
            if (File.Exists(_jsonFile))
            {
                string json = File.ReadAllText(_jsonFile);
                _users = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<ulong, User>>(json);
            }
            else
            {
                _users = new Dictionary<ulong, User>();
            }
        }

        private static async Task ChangeNickname(ulong userId)
        {
            if (_users.ContainsKey(userId))
            {
                foreach (ulong guildId in _users[userId].Guilds)
                {
                    IGuildUser user = await HelperFunctions.Instance().GetGuildUserByIds(guildId, userId);
                    await ChangeNickname(user);
                }
            }
        }

        private static async Task ChangeNickname(IGuildUser user)
        {
            if (_users.ContainsKey(user.Id))
            {
                string nickname = _users[user.Id].Name;
                if (!string.IsNullOrEmpty(nickname) && !string.IsNullOrEmpty(_users[user.Id].MainAccount))
                {
                    nickname += $" | ";
                }
                nickname += _users[user.Id].MainAccount;
                await user.ModifyAsync(p => p.Nickname = nickname);
            }
        }

        public static async Task ChangeUserName(ulong userId, string name)
        {
            if (_users.ContainsKey(userId))
            {
                _users[userId].Name = name;
                SaveUsers();
                await ChangeNickname(userId);
            }
        }

        public static string GetName(ulong userId)
        {
            if (_users.ContainsKey(userId))
            {
                return _users[userId].Name;
            }
            return string.Empty;
        }

        private class User
        {
            //AccountName, ApiKey
            public Dictionary<string, string> GuildWars2Accounts { get; set; }

            public string MainAccount { get; set; }

            public string Name { get; set; } = string.Empty;

            public List<ulong> Guilds { get; set; }

            public User()
            {
                GuildWars2Accounts = new Dictionary<string, string>();
                Guilds = new List<ulong>();
            }

            public User(string accountName, string apiKey)
            {
                GuildWars2Accounts = new Dictionary<string, string>();
                GuildWars2Accounts.Add(accountName, apiKey);
                Guilds = new List<ulong>();
                MainAccount = accountName;
            }
        }
    }
}
