using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Raidbot.Users.Accounts
{
    public class Account : IEquatable<string>
    {
        public string AccountName { get; set; }

        protected Account() { }

        public Account(string accountName)
        {
            AccountName = accountName;
        }

        public bool Equals([AllowNull] string other)
        {
            if (other == null) return false;
            return other == AccountName;
        }
    }
}
