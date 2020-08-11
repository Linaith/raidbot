using System;
using System.Collections.Generic;
using System.Text;

namespace Raidbot.Users.Accounts
{
    public class Account
    {
        public string AccountName { get; set; }

        protected Account() { }

        public Account(string accountName)
        {
            AccountName = accountName;
        }
    }
}
