using Discord;
using Raidbot.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Raidbot.Conversations
{
    class AccountRemoveConversation : ConversationBase
    {
        enum State { accountType, accountName }

        private readonly ulong _guildId;
        private State _state;
        private string _accountType;
        private readonly UserService _userService;

        private AccountRemoveConversation(ConversationService conversationService, UserService userService, IUser user, ulong guildId) : base(conversationService, user)
        {
            _guildId = guildId;
            _userService = userService;
            if (_userService.ListAccountTypes(guildId).Count == 1)
            {
                _state = State.accountName;
                _accountType = _userService.ListAccountTypes(guildId).First();
            }
            else
            {
                _state = State.accountType;
            }
        }

        public static async Task<AccountRemoveConversation> Create(ConversationService conversationService, UserService userService, IUser user, ulong guildId)
        {
            AccountRemoveConversation conversation = new AccountRemoveConversation(conversationService, userService, user, guildId);
            await conversation.SendInitialMessage();
            return conversation;
        }

        private async Task SendInitialMessage()
        {
            switch (_state)
            {
                case State.accountType:
                    await UserExtensions.SendMessageAsync(_user, $"From which account type do you want to remove an account?\n{_userService.PrintAccounts(_guildId, _user.Id)}");
                    break;
                case State.accountName:
                    await UserExtensions.SendMessageAsync(_user, $"Which {_accountType} account do you want to remove?\n{_userService.PrintAccounts(_guildId, _user.Id)}");
                    break;
            }
        }

        protected override async Task ProcessUncanceledMessage(string message)
        {
            switch (_state)
            {
                case State.accountType:
                    await ProcessAccountTypeAsync(message);
                    break;
                case State.accountName:
                    await ProcessAccountNameAsync(message);
                    break;
            }
        }

        public async Task ProcessAccountTypeAsync(string message)
        {
            if (_userService.ListAccountTypes(_guildId).Contains(message))
            {
                _accountType = message;
                await UserExtensions.SendMessageAsync(_user, $"Which {_accountType} account do you want to remove?");
                _state = State.accountName;
            }
            else
            {
                await UserExtensions.SendMessageAsync(_user, "Invalid account type. \n" +
                    "Please try again or type \"cancel\" to cancel the interaction.\n" +
                    $"Available account types: {CreateAccountTypeString()}");
            }
        }

        public async Task ProcessAccountNameAsync(string message)
        {
            if (_userService.RemoveAccount(_guildId, _user.Id, _accountType, message))
            {
                await UserExtensions.SendMessageAsync(_user, $"Removed the account successfully.\nYour accounts are:\n{_userService.PrintAccounts(_guildId, _user.Id)}");
                _conversationService.CloseConversation(_user.Id);
            }
            else
            {
                await UserExtensions.SendMessageAsync(_user, $"Account {message} not found. \n" +
                    "Please try again or type \"cancel\" to cancel the interaction.");
            }
        }

        private string CreateAccountTypeString()
        {
            string accountTypes = string.Empty;
            foreach (string accountType in _userService.ListAccountTypes(_guildId))
            {
                if (!string.IsNullOrEmpty(accountTypes))
                {
                    accountTypes += ", ";
                }
                accountTypes += $"{accountType}";
            }
            return accountTypes;
        }
    }
}
