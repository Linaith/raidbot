using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Raidbot
{
    public class CommandHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;

        // Retrieve client and CommandService instance via ctor
        public CommandHandler(DiscordSocketClient client, CommandService commands)
        {
            _commands = commands;
            _client = client;
        }

        public async Task InstallCommandsAsync()
        {
            // Hook the MessageReceived event into our command handler
            _client.MessageReceived += HandleCommandAsync;

            _client.ReactionAdded += HandleReactionAsync;

            _client.ReactionRemoved += HandleReactionRemovedAsync;

            _client.Log += LogAsync;

            _client.UserLeft += HandleUserLeft;

            // Here we discover all of the command modules in the entry 
            // assembly and load them. Starting from Discord.NET 2.0, a
            // service provider is required to be passed into the
            // module registration method to inject the 
            // required dependencies.
            //
            // If you do not use Dependency Injection, pass null.
            // See Dependency Injection guide for more information.
            await _commands.AddModulesAsync(assembly: Assembly.GetEntryAssembly(),
                                            services: null);
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
                services: null);
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
                    await raid.ManageUser(reaction, reaction.Emote);
                }
                if (DiscordRoles.IsRoleMessage(guild.Id, message.Id))
                {
                    await DiscordRoles.SetRole(guild, user, reaction.Emote);
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
                if (DiscordRoles.IsRoleMessage(guild.Id, message.Id))
                {
                    await DiscordRoles.UnsetRole(guild, user, reaction.Emote);
                }
            }
        }

        private async Task HandleUserLeft(SocketGuildUser user)
        {
            await Task.Run(() => PlannedRaids.RemoveUserFromAllRaids(user));
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
