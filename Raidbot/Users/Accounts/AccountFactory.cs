using System;

namespace Raidbot.Users.Accounts
{
    public class AccountFactory
    {
        public Account CreateAccount(string accountType, string accountDetails)
        {
            switch (accountType.ToLower())
            {
                case "guildwars2":
                    return new GuildWars2Account(accountDetails);
                default:
                    return new Account(accountDetails);
            }
        }
    }
}
