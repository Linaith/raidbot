using Discord;
using Discord.Commands;
using Raidbot.Users;
using System.Threading.Tasks;

namespace Raidbot.Modules
{
    [RequireRole("Raidlead")]
    [RequireContext(ContextType.Guild)]
    [RequireUserPermission(ChannelPermission.ManageRoles)]
    [Group("admin")]
    public class AdminModule : ModuleBase<SocketCommandContext>
    {
        [Command]
        [Summary("explains admin commands")]
        public async Task RaidHelpAsync()
        {
            string helpMessage = "existing admin commands:\n" +
                "!admin createrolemessage  -  Creates a message to allow users to pick their raid roles. Using this command again moves the message.\n" +
                "!admin deleterolemessage  -  Removes the role message and the user roles.\n" +
                "!admin addaccounttype <accountType> -  Adds a new AccountType to the server.\n" +
                "!admin removeaccounttype <accountType> -  Removed a new AccountType from the server.\n" +
                "!admin changenames <true | false> - activates or deactivates renaming the discord users.";
            await ReplyAsync(helpMessage);
        }

        [Command("createrolemessage")]
        [Summary("creates a role message")]
        public async Task CreateRoleMessageAsync()
        {
            if (Context.Channel is ITextChannel channel)
            {
                await DiscordRoles.PostRoleMessage(channel);
            }
        }

        [Command("deleterolemessage")]
        [Summary("deletes a role message")]
        public async Task DeleteRoleMessageAsync()
        {
            await DiscordRoles.DeleteRoleMessage(Context.Guild);
        }

        [Command("addaccounttype")]
        [Summary("adds a new account type to the Server")]
        public async Task AddAccountTypeAsync(string accountType)
        {
            UserManagement.AddAccountType(Context.Guild.Id, accountType);
            await ReplyAsync($"added account type: {accountType}");
        }

        [Command("removeaccounttype")]
        [Summary("removes an account type from the server")]
        public async Task RemoveAccountTypeAsync(string accountType)
        {
            if (UserManagement.RemoveAccountType(Context.Guild.Id, accountType))
            {
                await ReplyAsync($"removed account type: {accountType}");
            }
            else
            {
                await ReplyAsync($"removing account type {accountType} failed");
            }
        }

        [Command("changenames")]
        [Summary("toggles the ability of the bot to change names on the server")]
        public async Task ToggleChangeUserNameAsync(string changeName)
        {
            if (bool.TryParse(changeName, out bool change))
            {
                UserManagement.GetServer(Context.Guild.Id).ChangeNames = change;
                if (change)
                {
                    await ReplyAsync($"Names will now be managed by the bot.");
                }
                else
                {
                    await ReplyAsync($"Names will no longer be managed by the bot.");
                }
            }
            await ReplyAsync($"wrong parameter, only \"true\" or \"false\" allowed.");
        }
    }
}
