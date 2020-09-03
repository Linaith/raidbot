using Discord;
using Raidbot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Raidbot.Conversations
{
    class RaidEditRoleConversation : ConversationBase
    {
        enum State { Role, Name, Description }

        private readonly Raid _raid;
        private readonly IUserMessage _userMessage;
        private State _state;
        private string _role;
        private readonly RaidService _raidService;

        private RaidEditRoleConversation(ConversationService conversationService, RaidService raidService, IUser user, string raidId, IUserMessage userMessage) : base(conversationService, user)
        {
            this._state = State.Role;
            this._userMessage = userMessage;
            _raidService = raidService;
            if (!_raidService.TryFindRaid(raidId, out _raid)) throw new KeyNotFoundException("The raid for this message was not found!"); ;
        }

        public static async Task<RaidEditRoleConversation> Create(ConversationService conversationService, RaidService raidService, IUser user, string raidId, IUserMessage userMessage)
        {
            RaidEditRoleConversation conversation = new RaidEditRoleConversation(conversationService, raidService, user, raidId, userMessage);
            await conversation.SendAddAccountMessage();
            return conversation;
        }

        private async Task SendAddAccountMessage()
        {
            string sendMessage = $"You are editing the roles of Raid {_raid.RaidId}.\n" +
                $"you can write \"cancel\" to cancel the editing\n" +
                $"Which role do you want to edit? ({CreateRoleString()})";
            await UserExtensions.SendMessageAsync(_user, sendMessage);
        }

        protected override async Task ProcessUncanceledMessage(string message)
        {
            switch (_state)
            {
                case State.Role:
                    await ProcessRoleAsync(message);
                    break;
                case State.Name:
                    await ProcessNameAsync(message);
                    break;
                case State.Description:
                    await ProcessDescriptionAsync(message);
                    break;
            }
        }

        private async Task ProcessRoleAsync(string message)
        {
            if (_raid.Roles.Any(r => r.Name == message))
            {
                _role = message;
                await UserExtensions.SendMessageAsync(_user, $"Name for the role.");
                _state = State.Name;
            }
            else
            {
                await UserExtensions.SendMessageAsync(_user, "Invalid role name. \n" +
                    "Please try again or type \"cancel\" to cancel the interaction.\n" +
                    $"Available account types: {CreateRoleString()}");
            }
        }

        private async Task ProcessNameAsync(string message)
        {
            if (!_raid.Roles.Any(r => r.Name == message) || _role == message)
            {
                _raid.Roles.Find(r => r.Name == _role).Name = message;
                _role = message;
                await UserExtensions.SendMessageAsync(_user, $"Description of the role." +
                    $"\nOld description: " +
                    $"\n{_raid.Roles.Find(r => r.Name == _role).Description}");
                _state = State.Description;
            }
            else
            {
                await UserExtensions.SendMessageAsync(_user, $"A role with this name already exists. " +
                    $"\nPlease choose an other name or type \"cancel\" to cancel the interaction.");
            }
        }

        private async Task ProcessDescriptionAsync(string message)
        {
            _raid.Roles.Find(r => r.Name == _role).Description = message;
            try
            {
                await _userMessage.ModifyAsync(msg => msg.Embed = _raid.CreateRaidMessage());
                await UserExtensions.SendMessageAsync(_user, "Successfully modified the raid.");
            }
            catch { }
            finally
            {
                _conversationService.CloseConversation(_user.Id);
                _raidService.UpdateRaid(_raid.RaidId, _raid);
            }
        }

        private string CreateRoleString()
        {
            string roleString = string.Empty;
            foreach (string role in _raid.GetFreeRoles())
            {
                if (!string.IsNullOrEmpty(roleString))
                {
                    roleString += ", ";
                }
                roleString += role;
            }
            return roleString;
        }
    }
}
