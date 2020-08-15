using Raidbot.Users.Accounts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Raidbot.Users
{
    public class User
    {
        public Dictionary<string, List<Account>> GameAccounts { get; }

        public string MainAccount { get; private set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public ulong GuildId { get; }

        public string DiscordName
        {
            get
            {
                string nickname = Name;
                if (!string.IsNullOrEmpty(nickname) && !string.IsNullOrEmpty(MainAccount))
                {
                    nickname += $" | ";
                }
                nickname += MainAccount;
                return nickname;
            }
        }

        public User(ulong guildId)
        {
            GameAccounts = new Dictionary<string, List<Account>>();
            GuildId = guildId;
        }

        public bool AddAccount(string accountType, string accountDetails, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (!UserManagement.GetServer(GuildId).UsableAccountTypes.Contains(accountType))
            {
                errorMessage = "AccountType not supported.";
                return false;
            }
            try
            {
                AccountFactory factory = new AccountFactory();
                Account newAccount = factory.CreateAccount(accountType, accountDetails);

                if (GameAccounts.ContainsKey(accountType))
                {
                    foreach (Account account in GameAccounts[accountType])
                    {
                        if (account.AccountName == newAccount.AccountName)
                        {
                            GameAccounts[accountType].Remove(account);
                            break;
                        }
                    }
                }
                else
                {
                    GameAccounts.Add(accountType, new List<Account>());
                }
                GameAccounts[accountType].Add(newAccount);
                if (string.IsNullOrEmpty(MainAccount))
                {
                    MainAccount = newAccount.AccountName;
                }
                UserManagement.SaveUsers();
                return true;
            }
            catch (Exception e)
            {
                errorMessage = e.Message;
                return false;
            }
        }

        public bool RemoveAccount(string accountType, string accountName)
        {
            if (!GameAccounts.ContainsKey(accountType)) return false;

            int removed = GameAccounts[accountType].RemoveAll(a => a.AccountName == accountName);
            UserManagement.SaveUsers();

            return removed > 0;
        }

        public bool SetMainAccount(string mainAccount)
        {
            foreach (List<Account> accountList in GameAccounts.Values)
            {
                if (accountList.Any(m => m.AccountName == mainAccount))
                {
                    MainAccount = mainAccount;
                    UserManagement.SaveUsers();
                    return true;
                }
            }
            return false;
        }

        public string PrintAccounts()
        {
            string result = string.Empty;
            foreach (var accountEntry in GameAccounts)
            {
                result += accountEntry.Key + ":\n";
                foreach (Account account in accountEntry.Value)
                {
                    result += $"  {account.AccountName}\n";
                }
            }
            return result;
        }

        public List<Account> GetAccounts(string accountType)
        {
            if (GameAccounts.ContainsKey(accountType))
            {
                return GameAccounts[accountType];
            }
            return new List<Account>();
        }
    }
}
