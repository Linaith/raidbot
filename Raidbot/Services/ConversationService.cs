using Discord;
using Discord.WebSocket;
using Raidbot.Conversations;
using Raidbot.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Raidbot.Services
{
    public class ConversationService
    {
        private readonly UserService _userService;
        private readonly LogService _logService;
        private readonly Dictionary<ulong, IConversation> _conversations;

        public ConversationService(UserService userService, LogService logService)
        {
            _userService = userService;
            _logService = logService;
            _conversations = new Dictionary<ulong, IConversation>();
        }

        public bool UserHasConversation(ulong userId)
        {
            return _conversations.ContainsKey(userId);
        }

        public async void ProcessMessage(ulong userId, string message)
        {
            if (_conversations.ContainsKey(userId))
            {
                await _conversations[userId].ProcessMessage(message);
            }
        }

        public async void OpenAddAccountConversation(IUser user, ulong guildId)
        {
            _conversations.Add(user.Id, await AccountAddConversation.Create(this, _userService, user, guildId));
        }

        public async void OpenRemoveAccountConversation(IUser user, ulong guildId)
        {
            _conversations.Add(user.Id, await AccountRemoveConversation.Create(this, _userService, user, guildId));
        }

        public async void OpenSwitchAccountConversation(IUser user, ulong guildId)
        {
            _conversations.Add(user.Id, await AccountSwitchConversation.Create(this, _userService, user, guildId));
        }

        public async void OpenRaidCreateContinuousTextConversation(RaidService raidService, IUser user, IGuild guild, int frequency)
        {
            _conversations.Add(user.Id, await RaidCreateContinuousTextConversation.Create(this, raidService, _userService, user, guild, frequency));
        }

        public async void OpenRaidCreateConversation(RaidService raidService, IUser user, IGuild guild, int frequency)
        {
            _conversations.Add(user.Id, await RaidCreateConversation.Create(this, raidService, _userService, user, guild, frequency));
        }

        public async void OpenRaidEditConversation(RaidService raidService, IUser user, string raidId, RaidEditConversation.Edits edit, IUserMessage userMessage)
        {
            _conversations.Add(user.Id, await RaidEditConversation.Create(this, raidService, user, raidId, edit, userMessage));
        }

        public async void OpenRaidEditRoleConversation(RaidService raidService, IUser user, string raidId, IUserMessage userMessage)
        {
            _conversations.Add(user.Id, await RaidEditRoleConversation.Create(this, raidService, user, raidId, userMessage));
        }

        public async void OpenSignUpConversation(RaidService raidService, SocketReaction reaction, IGuildUser user, Raid raid, Constants.Availability availability)
        {
            _conversations.Add(user.Id, await SignUpConversation.Create(this, raidService, _userService, _logService, reaction, user, raid, availability));
        }

        public void CloseConversation(ulong userId)
        {
            _conversations.Remove(userId);
        }
    }
}
