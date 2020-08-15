using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Raidbot.Users.Accounts
{
    public class GuildWars2Account : Account
    {
        public string ApiKey { get; set; }
        private const string NEITHER_ACCOUNT_NOR_API = "The Argument was neither a Guild Wars 2 account name nor an valid Guild Wars 2 api key.";

        public GuildWars2Account(string accountDetails)
        {
            if (Regex.IsMatch(accountDetails, Constants.ACCOUNT_REGEX))
            {
                AccountName = accountDetails;
                return;
            }

            try
            {
                var connection = new Gw2Sharp.Connection(accountDetails);
                using var client = new Gw2Sharp.Gw2Client(connection);
                var webApiClient = client.WebApi.V2;

                var account = webApiClient.Account.GetAsync().Result;

                if (account == null)
                {
                    throw new ArgumentException(NEITHER_ACCOUNT_NOR_API);
                }
                AccountName = account.Name;
                ApiKey = accountDetails;
            }
            catch
            {
                throw new ArgumentException(NEITHER_ACCOUNT_NOR_API);
            }
        }
    }
}
