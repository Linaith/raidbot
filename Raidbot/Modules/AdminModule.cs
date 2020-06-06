using Discord;
using Discord.Commands;
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
                "!admin updateusernames  -  updates all usernames on the server.";
            await ReplyAsync(helpMessage);
        }

        [Command("createrolemessage")]
        [Summary("creates a role message")]
        public async Task CreateRoleMessageAsync()
        {
            if (Context.Channel is ITextChannel)
            {
                await DiscordRoles.PostRoleMessage((ITextChannel)Context.Channel);
            }
        }

        [Command("deleterolemessage")]
        [Summary("deletes a role message")]
        public async Task DeleteRoleMessageAsync()
        {
            await DiscordRoles.DeleteRoleMessage(Context.Guild);
        }

        [Command("updateusernames")]
        [Summary("updates all usernames on the server")]
        public async Task UpdateUsernamesAsync()
        {
            await UserManagement.UpdateAllNamesAsync(Context.Guild);
        }
    }
}
