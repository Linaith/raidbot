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
                }
                SaveUsers();
            }
            catch
            {
                //TODO: exception handling?
            }
            return true;
        }

        public static async Task<bool> RemoveGuildWars2Account(ulong userId, string accountName, ulong? guildId = null)
        {
            if (_users.ContainsKey(userId))
            {
                bool result = _users[userId].GuildWars2Accounts.Remove(accountName);
                if (result && guildId != null)
                {
                    IGuildUser user = await HelperFunctions.Instance().GetGuildUserByIds((ulong)guildId, userId);
                    if (_users[userId].GuildWars2Accounts.Count > 0)
                    {
                        await user.ModifyAsync(p => p.Nickname = _users[userId].GuildWars2Accounts.First().Key);
                    }
                    else
                    {
                        //TODO: test
                        await user.ModifyAsync(p => p.Nickname = null);
                    }
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
                foreach (ulong guildId in _users[userId].Guilds)
                {
                    IGuildUser user = await HelperFunctions.Instance().GetGuildUserByIds(guildId, userId);
                    await user.ModifyAsync(p => p.Nickname = accountName);
                }
                return true;
            }
            return false;
        }

        public static async Task AddServerAsync(IGuildUser user)
        {
            if (_users.ContainsKey(user.Id))
            {
                _users[user.Id].Guilds.Add(user.Guild.Id);
                await user.ModifyAsync(p => p.Nickname = _users[user.Id].MainAccount);
            }
        }

        public static async Task UpdateNameAsync(IGuildUser user)
        {
            if (_users.ContainsKey(user.Id))
            {
                await user.ModifyAsync(p => p.Nickname = _users[user.Id].MainAccount);
            }
        }

        public static async Task ChangeAllNamesAsync(IGuild guild)
        {
            foreach (IGuildUser user in await guild.GetUsersAsync())
            {
                await AddServerAsync(user);
            }
        }

        public static string GetAccountByIndex(ulong userId, int index)
        {
            if (_users.ContainsKey(userId))
            {
                int i = 0;
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

        private class User
        {
            //AccountName, ApiKey
            public Dictionary<string, string> GuildWars2Accounts { get; set; }

            public string MainAccount { get; set; }

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
