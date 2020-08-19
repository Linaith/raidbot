using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Raidbot.Services;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Raidbot
{
    public class CommandHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private readonly IServiceProvider _services;
        private readonly RoleService _roleService;

        // Retrieve client and CommandService instance via ctor
        public CommandHandler(DiscordSocketClient client, CommandService commands, RoleService roleService, IServiceProvider services)
        {
            _commands = commands;
            _client = client;
            _services = services;
            _roleService = roleService;
        }

        public async Task InstallCommandsAsync()
        {
            // Hook the MessageReceived event into our command handler
            _client.MessageReceived += HandleCommandAsync;

            _client.ReactionAdded += HandleReactionAsync;

            _client.ReactionRemoved += HandleReactionRemovedAsync;

            _client.Log += LogAsync;

            _client.UserLeft += HandleUserLeft;

            _client.GuildMemberUpdated += HandleGuildMemberUpdated;

            _client.MessageDeleted += HandleMessageDeleted;


            await _commands.AddModulesAsync(assembly: Assembly.GetEntryAssembly(),
                                            services: _services);
        }

        private async Task HandleMessageDeleted(Cacheable<IMessage, ulong> message, ISocketMessageChannel channel)
        {
            _roleService.DeleteMessage(message.Id);
        }

        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            // Don't process the command if it was a system message
            if (!(messageParam is SocketUserMessage message)) return;

            // Create a number to track where the prefix ends and the command begins
            int argPos = 0;

            // Create a WebSocket-based command context based on the message
            var context = new SocketCommandContext(_client, message);

            //Process MEssage for open conversation, if it is written in a private chat
            if (Program.Conversations.ContainsKey(context.User.Username) && context.IsPrivate)
            {
                Program.Conversations[context.User.Username].ProcessMessage(message.Content);
            }

            if (IsBotMentioned(messageParam, context))
            {
                if (!message.HasMentionPrefix(_client.CurrentUser, ref argPos) || message.Content.Equals(_client.CurrentUser.Mention))
                {
                    //TODO: send some messages
                }
            }

            // Determine if the message is a command based on the prefix and make sure no bots trigger commands
            if (!(message.HasCharPrefix('!', ref argPos) ||
                message.HasMentionPrefix(_client.CurrentUser, ref argPos)) ||
                message.Author.IsBot)
                return;

            // Execute the command with the command context we just
            // created, along with the service provider for precondition checks.
            await _commands.ExecuteAsync(
                context: context,
                argPos: argPos,
                services: _services);
        }

        private bool IsBotMentioned(SocketMessage messageParam, SocketCommandContext context)
        {
            foreach (SocketUser user in messageParam.MentionedUsers)
            {
                if (user.IsBot && user.Username.Equals(context.Client.CurrentUser.Username))
                {
                    return true;
                }
            }
            return false;
        }

        private async Task HandleReactionAsync(Cacheable<IUserMessage, ulong> cacheable, ISocketMessageChannel channel, SocketReaction reaction)
        {
            var message = await reaction.Channel.GetMessageAsync(reaction.MessageId);

            //check if the reaction was on a bot message
            if (!message.Author.Id.Equals(_client.CurrentUser.Id)) return;
            if (!reaction.User.IsSpecified || reaction.User.Value.IsBot) return;

            if (message.Channel.GetType() == typeof(SocketTextChannel))
            {
                IGuild guild = ((SocketGuildChannel)message.Channel).Guild;
                IGuildUser user = await guild.GetUserAsync(reaction.UserId);
                if (PlannedRaids.TryFindRaid(guild.Id, message.Channel.Id, message.Id, out Raid raid))
                {
                    await raid.ManageUser(reaction, reaction.Emote, guild.Id);
                }
                if (_roleService.IsRoleMessage(message.Id))
                {
                    await _roleService.SetRole(guild, user, reaction);
                }
            }
            else if (message.Channel.GetType() == typeof(SocketDMChannel))
            {
                //if it is a private message and contains an embed its probably a raid review
                if (message.Embeds.Count > 0 && Program.Conversations.ContainsKey(reaction.User.Value.Username))
                {
                    Program.Conversations[reaction.User.Value.Username].ProcessMessage(reaction.Emote.Name);
                }
            }
            // UserExtensions.SendMessageAsync(reaction.User.Value, reaction.Emote.ToString());//echo emote
        }

        private async Task HandleReactionRemovedAsync(Cacheable<IUserMessage, ulong> cacheable, ISocketMessageChannel channel, SocketReaction reaction)
        {
            var message = await reaction.Channel.GetMessageAsync(reaction.MessageId);

            //check if the reaction was on a bot message
            if (!message.Author.Id.Equals(_client.CurrentUser.Id)) return;
            if (!reaction.User.IsSpecified || reaction.User.Value.IsBot) return;

            if (message.Channel.GetType() == typeof(SocketTextChannel))
            {
                IGuild guild = ((SocketGuildChannel)message.Channel).Guild;
                IGuildUser user = await guild.GetUserAsync(reaction.UserId);
                if (_roleService.IsRoleMessage(message.Id))
                {
                    await _roleService.UnsetRole(guild, user, reaction);
                }
            }
        }

        private async Task HandleUserLeft(SocketGuildUser user)
        {
            await Task.Run(() => PlannedRaids.RemoveUserFromAllRaids(user));
        }

        private async Task HandleGuildMemberUpdated(SocketGuildUser oldUser, SocketGuildUser newUser)
        {
            if (Users.UserManagement.GetServer(newUser.Guild.Id).GetUser(newUser.Id).DiscordName != newUser.Nickname)
            {
                await Users.UserManagement.UpdateNameAsync(newUser);
            }
        }

        public async Task LogAsync(LogMessage logMessage)
        {
            // This casting type requries C#7
            if (logMessage.Exception is CommandException cmdException)
            {
                // We can tell the user that something unexpected has happened
                await cmdException.Context.Channel.SendMessageAsync("Something went catastrophically wrong!");

                // We can also log this incident
                Console.WriteLine($"{cmdException.Context.User} failed to execute '{cmdException.Command.Name}' in {cmdException.Context.Channel}.");
                Console.WriteLine(cmdException.ToString());
            }
        }
    }
}
