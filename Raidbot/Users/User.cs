using Raidbot.Users.Accounts;
using System.Collections.Generic;

namespace Raidbot.Users
{
    public class User
    {
        public Dictionary<string, List<Account>> GameAccounts { get; }

        public string MainAccount { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string GuildNickname
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

        public User()
        {
            GameAccounts = new Dictionary<string, List<Account>>();
        }
    }
}
