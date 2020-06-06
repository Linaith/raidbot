using Discord;
using System;
using System.Threading.Tasks;

namespace Raidbot.Conversations
{
    class ApiSwitchConversation : IConversation
    {
        private readonly IUser _user;

        private ApiSwitchConversation(IUser user)
        {
            _user = user;
        }

        public static async Task<ApiSwitchConversation> Create(IUser user)
        {
            await UserExtensions.SendMessageAsync(user, CreateApiRemoveMessage(user.Id));
            return new ApiSwitchConversation(user);
        }

        private static string CreateApiRemoveMessage(ulong userId)
        {
            string sendMessage = "Which account should be your main account?";
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

            if (await UserManagement.ChangeMainAccount(_user.Id, message))
            {
                await UserExtensions.SendMessageAsync(_user, $"The account \"{message}\" is now your main account");
            }
            else
            {
                await UserExtensions.SendMessageAsync(_user, $"No Account named \"{message}\" found");
            }
        }
    }
}
