﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Raidbot.Services;
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
        private readonly RoleService _roleService;
        private readonly UserService _userService;

        public AdminModule(RoleService roleService, UserService userService)
        {
            _roleService = roleService;
            _userService = userService;
        }

        [Command]
        [Summary("explains admin commands")]
        public async Task RaidHelpAsync()
        {
            string helpMessage = "existing admin commands:\n" +
                "!admin createrolemessage  -  Creates a message to allow users to pick their raid roles. Using this command again moves the message.\n" +
                "!admin deleterolemessage  -  Removes the role message and the user roles.\n" +
                "!admin addaccounttype <accountType> -  Adds a new AccountType to the server.\n" +
                "!admin removeaccounttype <accountType> -  Removed a new AccountType from the server.\n" +
                "!admin changenames <true | false> - activates or deactivates renaming the discord users.\n" +
                "!admin setlogchannel <#channel> - activates channel logging.\n" +
                "!admin removelogchannel - deactivates channel logging.";
            await ReplyAsync(helpMessage);
        }

        [Command("createrolemessage")]
        [Summary("creates a role message")]
        public async Task CreateRoleMessageAsync(string messageType)
        {
            if (Context.Channel is ITextChannel channel)
            {
                await _roleService.PostMessage(channel, messageType);
            }
        }

        [Command("addaccounttype")]
        [Summary("adds a new account type to the Server")]
        public async Task AddAccountTypeAsync(string accountType)
        {
            _userService.AddAccountType(Context.Guild.Id, accountType);
            await ReplyAsync($"added account type: {accountType}");
        }

        [Command("removeaccounttype")]
        [Summary("removes an account type from the server")]
        public async Task RemoveAccountTypeAsync(string accountType)
        {
            if (_userService.RemoveAccountType(Context.Guild.Id, accountType))
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
                _userService.GetServer(Context.Guild.Id).ChangeNames = change;
                if (change)
                {
                    await ReplyAsync($"Names will now be managed by the bot.");
                }
                else
                {
                    await ReplyAsync($"Names will no longer be managed by the bot.");
                }
            }
            else
            {
                await ReplyAsync($"wrong parameter, only \"true\" or \"false\" allowed.");
            }
        }

        [Command("setlogchannel")]
        [Summary("sets the log channel of the Server")]
        public async Task SetLogchannelAsync(string logChannel)
        {
            if (Context.Message.MentionedChannels.Count > 0)
            {
                foreach (SocketGuildChannel channel in Context.Message.MentionedChannels)
                {
                    if (channel is ITextChannel)
                    {
                        _userService.SetLogChannelId(Context.Guild.Id, channel.Id);
                        await ReplyAsync($"set log channel to {channel.Name}");
                        return;
                    }
                    else
                    {
                        await ReplyAsync($"channel {channel.Name} is not a text channel.");
                    }
                }
            }
            else
            {
                await ReplyAsync($"no mentiones channel found");
            }
        }

        [Command("removelogchannel")]
        [Summary("removes the log channel of the Server")]
        public async Task RemoveLogchannelAsync()
        {
            _userService.RemoveLogChannelId(Context.Guild.Id);
            await ReplyAsync($"disabled the channel logging");
        }
    }
}
