using Discord;
using Raidbot.Services;
using System.Linq;
using System.Threading.Tasks;

namespace Raidbot.Conversations
{
    class AccountAddConversation : ConversationBase
    {
        enum State { accountType, accountName }

        private readonly ulong _guildId;
        private State _state;
        private string _accountType;
        private readonly UserService _userService;

        private AccountAddConversation(ConversationService conversationService, UserService userService, IUser user, ulong guildId) : base(conversationService, user)
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

        public static async Task<AccountAddConversation> Create(ConversationService conversationService, UserService userService, IUser user, ulong guildId)
        {
            AccountAddConversation conversation = new AccountAddConversation(conversationService, userService, user, guildId);
            await conversation.SendAddAccountMessage();
            return conversation;
        }

        private async Task SendAddAccountMessage()
        {
            switch (_state)
            {
                case State.accountType:
                    await UserExtensions.SendMessageAsync(_user, $"For which account type do you want an account?\nAvailable account types: {CreateAccountTypeString()}");
                    break;
                case State.accountName:
                    await UserExtensions.SendMessageAsync(_user, $"Which {_accountType} account do you want to add?");
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
                await UserExtensions.SendMessageAsync(_user, $"Which {_accountType} account do you want to add?");
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
            if (_userService.AddAccount(_guildId, _user.Id, _accountType, message, out string errorMessage))
            {
                await UserExtensions.SendMessageAsync(_user, $"Added the account successfully.\nYour accounts are:\n{_userService.PrintAccounts(_guildId, _user.Id)}");
                _conversationService.CloseConversation(_user.Id);
            }
            else
            {
                await UserExtensions.SendMessageAsync(_user, $"{errorMessage}\n" +
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
