using Discord;
using Raidbot.Services;
using System;
using System.Threading.Tasks;

namespace Raidbot.Conversations
{
    public abstract class ConversationBase : IConversation
    {
        protected readonly ConversationService _conversationService;
        protected readonly IUser _user;

        public ConversationBase(ConversationService conversationService, IUser user)
        {
            _conversationService = conversationService;
            _user = user;
        }

        public async Task ProcessMessage(string message)
        {
            if (message.Equals("cancel", StringComparison.OrdinalIgnoreCase))
            {
                await UserExtensions.SendMessageAsync(_user, "interaction canceled");
                _conversationService.CloseConversation(_user.Id);
                return;
            }
            await ProcessUncanceledMessage(message);
        }

        protected abstract Task ProcessUncanceledMessage(string message);
    }
}
