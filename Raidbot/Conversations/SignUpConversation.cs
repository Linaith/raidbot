using Discord;
using Discord.WebSocket;
using Raidbot.Models;
using Raidbot.Services;
using System;
using System.Linq;
using System.Threading.Tasks;


namespace Raidbot.Conversations
{
    class SignUpConversation : ConversationBase
    {
        enum State { role, account }
        private new readonly IGuildUser _user;
        private readonly ISocketMessageChannel _channel;
        private readonly Raid _raid;
        private readonly Constants.Availability _availability;
        private string _role;
        private string _usedAccount;
        private State _state;
        private readonly RaidService _raidService;
        private readonly UserService _userService;
        private readonly LogService _logService;

        private SignUpConversation(ConversationService conversationService, RaidService raidService, UserService userService, LogService logService, ISocketMessageChannel channel, IGuildUser user, Raid raid, Constants.Availability availability) : base(conversationService, user)
        {
            _user = user;
            _channel = channel;
            this._raid = raid;
            this._availability = availability;
            _raidService = raidService;
            _userService = userService;
            _state = State.role;
            _logService = logService;
        }

        public static async Task<SignUpConversation> Create(ConversationService conversationService, RaidService raidService, UserService userService, LogService logService, SocketReaction reaction, IGuildUser user, Raid raid, Constants.Availability availability)
        {
            //Create Conversation
            SignUpConversation conversation = new SignUpConversation(conversationService, raidService, userService, logService, reaction.Channel, user, raid, availability);

            //remiove reaction
            //IUserMessage userMessage = (IUserMessage)await conversation._channel.GetMessageAsync(conversation._raid.MessageId);
            //await userMessage.RemoveReactionAsync(reaction.Emote, conversation._user);

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

        private string CreateAccountSelectionMessage()
        {
            string sendMessage = "Which account do you want to use for the Raid?\n" +
                "\nregistered accounts:";
            int i = 1;
            foreach (Users.Accounts.Account account in _userService.GetAccounts(_raid.GuildId, _user.Id, _raid.AccountType))
            {
                sendMessage += $"\n\t\t{i}: {account.AccountName}";
                i++;
            }
            sendMessage += "\ntype cancel to cancel account selection.";
            return sendMessage;
        }

        //case insensitive
        protected override async Task ProcessUncanceledMessage(string message)
        {
            switch (_state)
            {
                case State.role:
                    if (_raid.CheckRoleAvailability(_user.Id, message, _availability, out string resultMessage))
                    {
                        _role = message;

                        if (_userService.GetAccounts(_raid.GuildId, _user.Id, _raid.AccountType).Count() > 1)
                        {
                            await UserExtensions.SendMessageAsync(_user, CreateAccountSelectionMessage());
                            _state = State.account;
                        }
                        else
                        {
                            _usedAccount = _userService.GetAccounts(_raid.GuildId, _user.Id, _raid.AccountType).FirstOrDefault().AccountName;
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
                    if (int.TryParse(message, out int i) && _userService.GetAccounts(_raid.GuildId, _user.Id, _raid.AccountType).Count >= i && i > 0)
                    {
                        _usedAccount = _userService.GetAccounts(_raid.GuildId, _user.Id, _raid.AccountType)[i - 1].AccountName;
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

            if (_raidService.AddUser(_raid.RaidId, _user, _role, _availability, _usedAccount, out string resultMessage))
            {
                try
                {
                    await UserExtensions.SendMessageAsync(_user, resultMessage);
                    IUserMessage userMessage = (IUserMessage)await _channel.GetMessageAsync(_raid.MessageId);
                    await userMessage.ModifyAsync(msg => msg.Embed = _raid.CreateRaidMessage());
                    await _logService.LogRaid($"{_raid.Users[_user.Id].Nickname} signed up as {_availability}", _raid);
                }
                catch { }
                finally
                {
                    _conversationService.CloseConversation(_user.Id);
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
