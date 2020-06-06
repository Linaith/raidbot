using Discord;
using Discord.Commands;
using System.Threading.Tasks;


namespace Raidbot.Modules
{
    [RequireContext(ContextType.Guild)]
    [Group("user")]
    public class UserModule : ModuleBase<SocketCommandContext>
    {
        [Command]
        [Summary("explains user commands")]
        public async Task RaidHelpAsync()
        {
            string helpMessage = "existing user commands:\n" +
                "!user add api <ApiKey>  -  Adds an apikey to your user. This is currently only used for account names.\n" +
                "!user add account <AccountName> -  Adds an account without apikey to your user.\n" +
                "!user remove <AccountName>  -  Removes the account from your user.\n" +
                "!user change <AccountNAme>  -  Changes your main account. This affects your discorn Name on the Server.\n" +
                "!user list  -  Lists all your accounts.\n" +
                "!user update  -  Updates the Username to match the main account." +
                "*Accounts containing a space caracter have to be placed in quatation marks. e.g.: \n!user change \"Test Account.1234\"*";
            await ReplyAsync(helpMessage);
        }

        [Command("remove")]
        [Summary("remove an account")]
        public async Task RemoveApiKeyAsync(string accountName)
        {
            if (await UserManagement.RemoveGuildWars2Account(Context.User.Id, accountName, Context.Guild?.Id))
            {
                await Context.Channel.SendMessageAsync("Account removed successfully.");
            }
            else
            {
                await Context.Channel.SendMessageAsync("Account was not found.");
            }
        }

        [Command("change")]
        [Summary("switch main account")]
        public async Task SwitchAccountAsync(string accountName)
        {
            if (await UserManagement.ChangeMainAccount(Context.User.Id, accountName))
            {
                await Context.Channel.SendMessageAsync("Main account changed successfully.");
            }
            else
            {
                await Context.Channel.SendMessageAsync("Account was not found.");
            }
        }

        [Command("list")]
        [Summary("list linked accounts")]
        public async Task ListAccountsAsync()
        {
            string accounts = string.Empty;
            foreach (string account in UserManagement.GetGuildWars2AccountNames(Context.User.Id))
            {
                accounts += $"{account}\n";
            }

            await Context.Channel.SendMessageAsync(accounts); ;
        }

        [Command("update")]
        [Summary("list linked accounts")]
        public async Task UpdateAsync()
        {
            UserManagement.AddServer((IGuildUser)Context.User);
            await UserManagement.UpdateNameAsync((IGuildUser)Context.User);
        }

        [Group("add")]
        public class RaidEdit : ModuleBase<SocketCommandContext>
        {
            [Command("api")]
            [Summary("add an api key")]
            public async Task AddApiKeyAsync(string apiKey)
            {
                await UserManagement.AddGuildwars2ApiKey(Context.User.Id, apiKey, Context.Guild?.Id);
                await Context.Message.DeleteAsync();
                await Context.Channel.SendMessageAsync("Apikey was added.");
            }

            [Command("account")]
            [Summary("add an api key")]
            public async Task AddAccountAsync(string accountname)
            {
                if (await UserManagement.AddGuildwars2Account(Context.User.Id, accountname, Context.Guild?.Id))
                {
                    await Context.Channel.SendMessageAsync("Account was added.");
                    return;
                }
                await Context.Channel.SendMessageAsync("Invalid account name.");
            }
        }
    }
}
