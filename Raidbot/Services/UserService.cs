using Discord;
using Raidbot.Users;
using Raidbot.Users.Accounts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Raidbot.Services
{
    public class UserService
    {
        private readonly string _jsonFile = Path.Combine(Constants.SAVEFOLDER, "users.json");

        //guildId, Server
        private Dictionary<ulong, DiscordServer> _discordServers;

        public UserService()
        {
            LoadUsers();
        }

        public DiscordServer GetServer(ulong guildId)
        {
            if (!_discordServers.ContainsKey(guildId))
            {
                _discordServers.Add(guildId, new DiscordServer(guildId));
            }
            return _discordServers[guildId];
        }

        public void SaveUsers()
        {
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(_discordServers);
            File.WriteAllText(_jsonFile, json);
        }

        private void LoadUsers()
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

        public async Task UpdateNameAsync(IGuildUser user)
        {
            if (_discordServers.ContainsKey(user.GuildId) && _discordServers[user.GuildId].ChangeNames && _discordServers[user.GuildId].Users.ContainsKey(user.Id))
            {
                string nickname = GetUser(user.GuildId, user.Id).GuildNickname;
                if (!string.IsNullOrEmpty(nickname))
                {
                    await user.ModifyAsync(p => p.Nickname = nickname);
                }
            }
        }

        public void AddAccountType(ulong guildId, string accountType)
        {
            DiscordServer server = GetServer(guildId);
            if (!server.UsableAccountTypes.Contains(accountType))
            {
                server.UsableAccountTypes.Add(accountType);
                SaveUsers();
            }
        }

        public bool RemoveAccountType(ulong guildId, string accountType)
        {
            DiscordServer server = GetServer(guildId);
            if (server.UsableAccountTypes.Contains(accountType))
            {
                server.UsableAccountTypes.Remove(accountType);
                foreach (User user in server.Users.Values)
                {
                    if (user.GameAccounts.ContainsKey(accountType))
                    {
                        user.GameAccounts.Remove(accountType);
                    }
                }
                SaveUsers();
                return true;
            }
            return false;
        }

        public List<string> ListAccountTypes(ulong guildId)
        {
            return GetServer(guildId).UsableAccountTypes;
        }

        public void RemoveUser(ulong guildId, ulong userId)
        {
            GetServer(guildId).Users.Remove(userId);
            SaveUsers();
        }

        private User GetUser(ulong guildId, ulong userId)
        {
            DiscordServer server = GetServer(guildId);
            if (!server.Users.ContainsKey(userId))
            {
                User newUser = new User();
                server.Users.Add(userId, newUser);
                SaveUsers();
            }
            return server.Users[userId];
        }


        public bool AddAccount(ulong guildId, ulong userId, string accountType, string accountDetails, out string errorMessage)
        {
            DiscordServer server = GetServer(guildId);
            User user = GetUser(guildId, userId);
            errorMessage = string.Empty;
            if (!server.UsableAccountTypes.Contains(accountType))
            {
                errorMessage = "AccountType not supported.";
                return false;
            }
            try
            {
                AccountFactory factory = new AccountFactory();
                Account newAccount = factory.CreateAccount(accountType, accountDetails);

                if (user.GameAccounts.ContainsKey(accountType))
                {
                    foreach (Account account in user.GameAccounts[accountType])
                    {
                        if (account.AccountName == newAccount.AccountName)
                        {
                            user.GameAccounts[accountType].Remove(account);
                            break;
                        }
                    }
                }
                else
                {
                    user.GameAccounts.Add(accountType, new List<Account>());
                }
                user.GameAccounts[accountType].Add(newAccount);
                if (string.IsNullOrEmpty(user.MainAccount))
                {
                    user.MainAccount = newAccount.AccountName;
                }
                SaveUsers();
                return true;
            }
            catch (Exception e)
            {
                errorMessage = e.Message;
                return false;
            }
        }

        public bool RemoveAccount(ulong guildId, ulong userId, string accountType, string accountName)
        {
            User user = GetUser(guildId, userId);
            if (!user.GameAccounts.ContainsKey(accountType)) return false;

            int removed = user.GameAccounts[accountType].RemoveAll(a => a.AccountName == accountName);
            SaveUsers();

            return removed > 0;
        }

        public bool SetMainAccount(ulong guildId, ulong userId, string mainAccount)
        {
            User user = GetUser(guildId, userId);
            foreach (List<Account> accountList in user.GameAccounts.Values)
            {
                if (accountList.Any(m => m.AccountName == mainAccount))
                {
                    user.MainAccount = mainAccount;
                    SaveUsers();
                    return true;
                }
            }
            return false;
        }

        public string PrintAccounts(ulong guildId, ulong userId)
        {
            User user = GetUser(guildId, userId);
            string result = string.Empty;
            foreach (var accountEntry in user.GameAccounts)
            {
                result += accountEntry.Key + ":\n";
                foreach (Account account in accountEntry.Value)
                {
                    result += $"  {account.AccountName}\n";
                }
            }
            return result;
        }

        public List<Account> GetAccounts(ulong guildId, ulong userId, string accountType)
        {
            User user = GetUser(guildId, userId);
            if (user.GameAccounts.ContainsKey(accountType))
            {
                return user.GameAccounts[accountType];
            }
            return new List<Account>();
        }

        public void SetUserName(ulong guildId, ulong userId, string name)
        {
            User user = GetUser(guildId, userId);
            user.Name = name;
            SaveUsers();
        }

        public string GetUserName(ulong guildId, ulong userId)
        {
            return GetUser(guildId, userId).Name;
        }

        public string GetDiscordName(ulong guildId, ulong userId)
        {
            return GetUser(guildId, userId).GuildNickname;
        }
    }
}
