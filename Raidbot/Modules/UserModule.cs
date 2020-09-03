using Discord;
using Discord.Commands;
using Raidbot.Conversations;
using Raidbot.Services;
using Raidbot.Users;
using System.Linq;
using System.Threading.Tasks;


namespace Raidbot.Modules
{
    [Group("user")]
    public class UserModule : ModuleBase<SocketCommandContext>
    {
        private readonly ConversationService _conversationService;
        private readonly UserService _userService;

        public UserModule(ConversationService conversationService, UserService userService)
        {
            _conversationService = conversationService;
            _userService = userService;
        }

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
            foreach (string accountType in _userService.ListAccountTypes(Context.Guild.Id))
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
            if (_userService.ListAccountTypes(Context.Guild.Id).Count == 0)
            {
                await Context.Channel.SendMessageAsync("No account types are specified for this Server.");
                return;
            }
            if (!_conversationService.UserHasConversation(Context.User.Id))
            {
                _conversationService.OpenAddAccountConversation(Context.User, Context.Guild.Id);
            }
        }

        [Command("add")]
        [Summary("add an account")]
        public async Task AddAccountAsync(string accountType, string accountDetails)
        {
            if (!_userService.ListAccountTypes(Context.Guild.Id).Contains(accountType))
            {
                await Context.Channel.SendMessageAsync($"Account type {accountType} not found.");
                return;
            }
            if (_userService.AddAccount(Context.Guild.Id, Context.User.Id, accountType, accountDetails, out string errorMessage))
            {
                await Context.Channel.SendMessageAsync($"Account {accountDetails} was added to your {accountDetails} accounts.");
                return;
            }
            await Context.Channel.SendMessageAsync(errorMessage);
            await Context.Message.DeleteAsync();
        }

        [Command("remove")]
        [Summary("remove an account")]
        public async Task RemoveAccountAsync()
        {
            if (_userService.ListAccountTypes(Context.Guild.Id).Count == 0)
            {
                await Context.Channel.SendMessageAsync("No account types are specified for this Server.");
                return;
            }
            if (!_conversationService.UserHasConversation(Context.User.Id))
            {
                _conversationService.OpenRemoveAccountConversation(Context.User, Context.Guild.Id);
            }
        }

        [Command("remove")]
        [Summary("remove an account")]
        public async Task RemoveAccountAsync(string accountType, string accountName)
        {
            if (!_userService.ListAccountTypes(Context.Guild.Id).Contains(accountType))
            {
                await Context.Channel.SendMessageAsync($"Account type {accountType} not found.");
                return;
            }
            if (_userService.RemoveAccount(Context.Guild.Id, Context.User.Id, accountType, accountName))
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
            if (_userService.ListAccountTypes(Context.Guild.Id).Count == 0)
            {
                await Context.Channel.SendMessageAsync("No account types are specified for this Server.");
                return;
            }
            if (!_conversationService.UserHasConversation(Context.User.Id))
            {
                _conversationService.OpenSwitchAccountConversation(Context.User, Context.Guild.Id);
            }
        }

        [Command("change")]
        [Summary("switch main account")]
        public async Task SwitchAccountAsync(string accountName)
        {
            if (_userService.SetMainAccount(Context.Guild.Id, Context.User.Id, accountName))
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
            _userService.SetUserName(Context.Guild.Id, Context.User.Id, name);
            await _userService.UpdateNameAsync((IGuildUser)Context.User);
            await Context.Channel.SendMessageAsync("Your name was changed");
        }

        [Command("list")]
        [Summary("list linked accounts")]
        public async Task ListAccountsAsync()
        {
            await Context.Channel.SendMessageAsync(_userService.PrintAccounts(Context.Guild.Id, Context.User.Id));
        }

        [Command("update")]
        [Summary("list linked accounts")]
        public async Task UpdateAsync()
        {
            await _userService.UpdateNameAsync((IGuildUser)Context.User);
            await Context.Channel.SendMessageAsync("Your name was updated");
        }

        [Command("repair")]
        [Summary("list linked accounts")]
        public async Task RepairAsync()
        {
            foreach (var user in _userService.GetServer(Context.Guild.Id).Users)
            {
                try
                {
                    if (string.IsNullOrEmpty(user.Value.MainAccount))
                    {
                        _userService.SetMainAccount(Context.Guild.Id, user.Key, user.Value.GameAccounts.Values.First().First().AccountName);
                        await _userService.UpdateNameAsync(Context.Guild.Users.Where(x => x.Id == user.Key).First());
                    }
                }
                catch
                {
                    await Context.Channel.SendMessageAsync($"failed repairing {user.Key}");
                }
            }
        }
    }
}
