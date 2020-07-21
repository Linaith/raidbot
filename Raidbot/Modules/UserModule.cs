using Discord;
using Discord.Commands;
using System.Threading.Tasks;


namespace Raidbot.Modules
{
    [Group("user")]
    public class UserModule : ModuleBase<SocketCommandContext>
    {
        [RequireContext(ContextType.DM | ContextType.Group)]
        [Command]
        [Summary("explains user commands")]
        public async Task ServerOnlyAsync(params string[] stuffToIgnore)
        {
            string helpMessage = "This command has to be executed on a Server.";
            await ReplyAsync(helpMessage);
        }

        [Command]
        [Summary("explains user commands")]
        public async Task RaidHelpAsync()
        {
            string helpMessage = "existing user commands:\n" +
                "!user add api <ApiKey>\n" +
                "Adds an apikey to your user. This is currently only used for account names.\n" +
                "The APi key needs account permission. Additional permissions may be required in future.\n\n" +
                "!user add account <AccountName>\nAdds an account without apikey to your user.\n\n" +
                "!user remove <AccountName>\nRemoves the account from your user.\n\n" +
                "!user change <AccountName>\nChanges your main account. This affects your discord Name on the Server.\n\n" +
                "!user list\nLists all your accounts.\n\n" +
                "!user name [Name]\nAdds your name to the Discord nickname. Deletes it without parameter.\n\n" +
                "!user update\nUpdates the Username to match the main account.\n\n" +
                "**Accounts containing a space caracter have to be placed in quotation marks. e.g.: \n!user change \"Test Account.1234\"**";
            await ReplyAsync(helpMessage);
        }

        [Command("name")]
        [Summary("change your name")]
        public async Task ChangeNameAsync(string name = "")
        {
            UserManagement.AddServer((IGuildUser)Context.User);
            await UserManagement.ChangeUserName(Context.User.Id, name);
            await Context.Channel.SendMessageAsync("Name was changed");
        }

        [Command("remove")]
        [Summary("remove an account")]
        public async Task RemoveApiKeyAsync(string accountName)
        {
            if (await UserManagement.RemoveGuildWars2Account(Context.User.Id, accountName))
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
