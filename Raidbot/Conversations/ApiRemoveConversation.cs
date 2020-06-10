using Discord;
using System;
using System.Threading.Tasks;

namespace Raidbot.Conversations
{
    class ApiRemoveConversation : IConversation
    {
        private readonly IUser _user;

        private ApiRemoveConversation(IUser user)
        {
            _user = user;
        }

        public static async Task<ApiRemoveConversation> Create(IUser user)
        {
            await UserExtensions.SendMessageAsync(user, CreateApiRemoveMessage(user.Id));
            return new ApiRemoveConversation(user);
        }

        private static string CreateApiRemoveMessage(ulong userId)
        {
            string sendMessage = "Which account do you want to rmove?";
            foreach (string account in UserManagement.GetGuildWars2AccountNames(userId))
            {
                sendMessage += $"\n{account}";
            }
            sendMessage += "\n\ntype cancel to cancel the interaction.";
            return sendMessage;
        }

        public async void ProcessMessage(string message)
        {
            if (message.Equals("cancel", StringComparison.OrdinalIgnoreCase))
            {
                await UserExtensions.SendMessageAsync(_user, "interaction canceled");
                Program.Conversations.Remove(_user.Username);
                return;
            }

            if (await UserManagement.RemoveGuildWars2Account(_user.Id, message))
            {
                await UserExtensions.SendMessageAsync(_user, $"The account \"{message}\" was removed successfully");
            }
            else
            {
                await UserExtensions.SendMessageAsync(_user, $"No Account named \"{message}\" found");
            }
        }
    }
}
