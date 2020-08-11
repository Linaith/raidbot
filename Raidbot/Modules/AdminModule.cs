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
                "!admin addaccounttype     -  Adds a new AccountType to the server." +
                "!admin removeaccounttype  -  Removed a new AccountType from the server.";
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
        [Summary("deletes a role message")]
        public async Task AddAccountTypeAsync(string accountType)
        {
            UserManagement.AddAccountType(Context.Guild.Id, accountType);
            await ReplyAsync($"added account type: {accountType}");
        }

        [Command("removeaccounttype")]
        [Summary("deletes a role message")]
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
    }
}
