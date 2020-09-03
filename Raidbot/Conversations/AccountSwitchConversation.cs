using Discord;
using Raidbot.Services;
using System;
using System.Threading.Tasks;

namespace Raidbot.Conversations
{
    class AccountSwitchConversation : ConversationBase
    {
        private readonly ulong _guildId;
        private readonly UserService _userService;

        private AccountSwitchConversation(ConversationService conversationService, UserService userService, IUser user, ulong guildId) : base(conversationService, user)
        {
            _guildId = guildId;
            _userService = userService;
        }

        public static async Task<AccountSwitchConversation> Create(ConversationService conversationService, UserService userService, IUser user, ulong guildId)
        {
            string message = "Which account should be your main account?";
            message += $"\n{userService.PrintAccounts(guildId, user.Id)}";
            message += "\n\ntype cancel to cancel the interaction.";
            await UserExtensions.SendMessageAsync(user, message);
            return new AccountSwitchConversation(conversationService, userService, user, guildId);
        }

        protected override async Task ProcessUncanceledMessage(string message)
        {
            if (_userService.SetMainAccount(_guildId, _user.Id, message))
            {
                await UserExtensions.SendMessageAsync(_user, $"The account \"{message}\" is now your main account");
                _conversationService.CloseConversation(_user.Id);
            }
            else
            {
                await UserExtensions.SendMessageAsync(_user, $"No Account named \"{message}\" found.\nPlease try again or type \"cancel\" to cancel the interaction.");
            }
        }
    }
}
