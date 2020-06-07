using Discord;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Threading.Tasks;
using static Raidbot.Raid;

namespace Raidbot
{
    class SignUpConversation : IConversation
    {
        enum State { role, account }
        private readonly IGuildUser _user;
        private readonly ISocketMessageChannel _channel;
        private readonly Raid _raid;
        private readonly Raid.Availability _availability;
        private string _role;
        private string _usedAccount;
        private State _state;

        private SignUpConversation(ISocketMessageChannel channel, IGuildUser user, Raid raid, Raid.Availability availability)
        {
            _user = user;
            _channel = channel;
            this._raid = raid;
            this._availability = availability;
            _state = State.role;
        }

        public static async Task<SignUpConversation> Create(SocketReaction reaction, Raid raid, Raid.Availability availability)
        {
            if (reaction.User.Value is IGuildUser user)
            {
                //Create Conversation
                SignUpConversation conversation = new SignUpConversation(reaction.Channel, user, raid, availability);

                //remiove reaction
                IUserMessage userMessage = (IUserMessage)await conversation._channel.GetMessageAsync(conversation._raid.MessageId);
                await userMessage.RemoveReactionAsync(reaction.Emote, conversation._user);

                //send sign up message
                if (reaction.Emote.Equals(Constants.FlexEmoji))
                {
                    await UserExtensions.SendMessageAsync(conversation._user, CreateFlexRoleMessage(conversation._raid));
                }
                else
                {
                    await UserExtensions.SendMessageAsync(conversation._user, CreateSignUpMessage(conversation._raid));
                }
                return conversation;
            }
            throw new Exception("User is no GuildUser");
        }

        private static string CreateSignUpMessage(Raid raid)
        {
            string sendMessage = "Pick a role(";
            bool start = true;
            foreach (string role in raid.GetFreeRoles())
            {
                if (!start) sendMessage += ", ";
                sendMessage += role;
                start = false;
            }
            sendMessage += ") or type cancel to cancel role selection.";
            return sendMessage;
        }

        private static string CreateFlexRoleMessage(Raid raid)
        {
            string sendMessage = "Pick a flex role(";
            bool start = true;
            foreach (Role role in raid.Roles)
            {
                if (!start) sendMessage += ", ";
                sendMessage += role.Name;
                start = false;
            }
            sendMessage += ") or type cancel to cancel role selection.";
            return sendMessage;
        }

        private static string CreateAccountSelectionMessage(ulong userId)
        {
            string sendMessage = "Which account do you want to use for the Raid?\n" +
                "\nregistered accounts:";
            int i = 1;
            foreach (string account in UserManagement.GetGuildWars2AccountNames(userId))
            {
                sendMessage += $"\n\t\t{i}: {account}";
                i++;
            }
            sendMessage += "\ntype cancel to cancel account selection.";
            return sendMessage;
        }

        //case insensitive
        public async void ProcessMessage(string message)
        {
            if (message.Equals("cancel", StringComparison.OrdinalIgnoreCase))
            {
                await UserExtensions.SendMessageAsync(_user, "interaction canceled");
                Program.Conversations.Remove(_user.Username);
                return;
            }

            switch (_state)
            {
                case State.role:
                    if (_raid.CheckRoleAvailability(_user.Id, message, _availability, out string resultMessage))
                    {
                        _role = message;
                        if (UserManagement.GetGuildWars2AccountNames(_user.Id).Count() > 1)
                        {
                            await UserExtensions.SendMessageAsync(_user, CreateAccountSelectionMessage(_user.Id));
                            _state = State.account;
                        }
                        else
                        {
                            _usedAccount = UserManagement.GetGuildWars2AccountNames(_user.Id).FirstOrDefault();
                            AddUser();
                        }
                    }
                    else
                    {
                        resultMessage += $"\n\n{CreateSignUpMessage(_raid)}";
                        await UserExtensions.SendMessageAsync(_user, resultMessage);
                    }
                    break;
                case State.account:
                    if (int.TryParse(message, out int i) && !string.IsNullOrEmpty(UserManagement.GetAccountByIndex(_user.Id, i)))
                    {
                        _usedAccount = UserManagement.GetAccountByIndex(_user.Id, i);
                        AddUser();
                    }
                    else
                    {
                        await UserExtensions.SendMessageAsync(_user, "Invalid numer, please select the account index.");
                    }
                    break;
            }
        }

        private async void AddUser()
        {

            if (_raid.AddUser(_user, _role, _availability, _usedAccount, out string resultMessage))
            {
                try
                {
                    await UserExtensions.SendMessageAsync(_user, resultMessage);
                    IUserMessage userMessage = (IUserMessage)await _channel.GetMessageAsync(_raid.MessageId);
                    await userMessage.ModifyAsync(msg => msg.Embed = _raid.CreateRaidMessage());
                }
                catch { }
                finally
                {
                    Program.Conversations.Remove(_user.Username);
                }
            }
            else
            {
                resultMessage += $"\n\n{CreateSignUpMessage(_raid)}";
                await UserExtensions.SendMessageAsync(_user, resultMessage);
            }
        }
    }
}
