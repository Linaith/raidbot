using Discord;
using Discord.Commands;
using Raidbot.Conversations;
using Raidbot.Users;
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
            string accountTypes = string.Empty;
            foreach (string accountType in UserManagement.GetServer(Context.Guild.Id).ListAccountTypes())
            {
                if (!string.IsNullOrEmpty(accountTypes))
                {
                    accountTypes += ", ";
                }
                accountTypes += $"{accountType}";
            }
            string helpMessage = "existing user commands:\n" +
                "!user add \nOpens an add account conversation.\n\n" +
                "!user add <AccountType> <AccountName>\nAdds an account with given account type and name.\n\n" +
                "!user remove\nOpens an remove account conversation.\n\n" +
                "!user remove <AccountType> <AccountName>\nRemoves the account from your user.\n\n" +
                "!user change\nOpens an change account conversation.\n\n" +
                "!user change <AccountName>\nChanges your main account. This affects your discord Name on the Server.\n\n" +
                "!user list\nLists all your accounts.\n\n" +
                "!user name [Name]\nAdds your name to the Discord nickname. Deletes it without parameter.\n\n" +
                "!user update\nUpdates the Username to match the main account.\n\n" +
                $"Available accountTypes: {accountTypes}\n\n" +
                "**Accounts containing a space caracter have to be placed in quotation marks. e.g.: \n!user change \"Test Account.1234\"**";
            await ReplyAsync(helpMessage);
        }

        [Command("add")]
        [Summary("add an account")]
        public async Task AddAccountAsync()
        {
            if (UserManagement.GetServer(Context.Guild.Id).ListAccountTypes().Count == 0)
            {
                await Context.Channel.SendMessageAsync("No account types are specified for this Server.");
                return;
            }
            if (!Program.Conversations.ContainsKey(Context.User.Username))
            {
                Program.Conversations.Add(Context.User.Username, await AccountAddConversation.Create(Context.User, Context.Guild.Id));
            }
        }

        [Command("add")]
        [Summary("add an account")]
        public async Task AddAccountAsync(string accountType, string accountDetails)
        {
            await Context.Message.DeleteAsync();
            if (!UserManagement.GetServer(Context.Guild.Id).ListAccountTypes().Contains(accountType))
            {
                await Context.Channel.SendMessageAsync($"Account type {accountType} not found.");
                return;
            }
            if (UserManagement.GetServer(Context.Guild.Id).GetUser(Context.User.Id).AddAccount(accountType, accountDetails, out string errorMessage))
            {
                await Context.Channel.SendMessageAsync($"Account {accountDetails} was added to your {accountDetails} accounts.");
                return;
            }
            await Context.Channel.SendMessageAsync(errorMessage);
        }

        [Command("remove")]
        [Summary("remove an account")]
        public async Task RemoveAccountAsync()
        {
            if (UserManagement.GetServer(Context.Guild.Id).ListAccountTypes().Count == 0)
            {
                await Context.Channel.SendMessageAsync("No account types are specified for this Server.");
                return;
            }
            if (!Program.Conversations.ContainsKey(Context.User.Username))
            {
                Program.Conversations.Add(Context.User.Username, await AccountRemoveConversation.Create(Context.User, Context.Guild.Id));
            }
        }

        [Command("remove")]
        [Summary("remove an account")]
        public async Task RemoveAccountAsync(string accountType, string accountName)
        {
            if (!UserManagement.GetServer(Context.Guild.Id).ListAccountTypes().Contains(accountType))
            {
                await Context.Channel.SendMessageAsync($"Account type {accountType} not found.");
                return;
            }
            if (UserManagement.GetServer(Context.Guild.Id).GetUser(Context.User.Id).RemoveAccount(accountType, accountName))
            {
                await Context.Channel.SendMessageAsync("Account removed successfully.");
                return;
            }
            await Context.Channel.SendMessageAsync("Account was not found.");
        }

        [Command("change")]
        [Summary("switch main account")]
        public async Task SwitchAccountAsync()
        {
            if (UserManagement.GetServer(Context.Guild.Id).ListAccountTypes().Count == 0)
            {
                await Context.Channel.SendMessageAsync("No account types are specified for this Server.");
                return;
            }
            if (!Program.Conversations.ContainsKey(Context.User.Username))
            {
                Program.Conversations.Add(Context.User.Username, await AccountSwitchConversation.Create(Context.User, Context.Guild.Id));
            }
        }

        [Command("change")]
        [Summary("switch main account")]
        public async Task SwitchAccountAsync(string accountName)
        {
            if (UserManagement.GetServer(Context.Guild.Id).GetUser(Context.User.Id).SetMainAccount(accountName))
            {
                await Context.Channel.SendMessageAsync("Main account changed successfully.");
                return;
            }
            await Context.Channel.SendMessageAsync("Account was not found.");
        }

        [Command("name")]
        [Summary("change your name")]
        public async Task ChangeNameAsync(string name = "")
        {
            UserManagement.GetServer(Context.Guild.Id).GetUser(Context.User.Id).Name = name;
            await UserManagement.UpdateNameAsync((IGuildUser)Context.User);
            await Context.Channel.SendMessageAsync("Your name was changed");
        }

        [Command("list")]
        [Summary("list linked accounts")]
        public async Task ListAccountsAsync()
        {
            await Context.Channel.SendMessageAsync(UserManagement.GetServer(Context.Guild.Id).GetUser(Context.User.Id).PrintAccounts());
        }

        [Command("update")]
        [Summary("list linked accounts")]
        public async Task UpdateAsync()
        {
            await UserManagement.UpdateNameAsync((IGuildUser)Context.User);
            await Context.Channel.SendMessageAsync("Your name was updated");
        }
    }
}
