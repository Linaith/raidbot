using Discord;
using Raidbot.Users;
using System;
using System.Threading.Tasks;

namespace Raidbot.Conversations
{
    class AccountSwitchConversation : IConversation
    {
        private readonly IUser _user;
        private readonly ulong _guildId;

        private AccountSwitchConversation(IUser user, ulong guildId)
        {
            _user = user;
            _guildId = guildId;
        }

        public static async Task<AccountSwitchConversation> Create(IUser user, ulong guildId)
        {
            await UserExtensions.SendMessageAsync(user, CreateApiRemoveMessage(user.Id, guildId));
            return new AccountSwitchConversation(user, guildId);
        }

        private static string CreateApiRemoveMessage(ulong userId, ulong guildId)
        {
            string sendMessage = "Which account should be your main account?";
            sendMessage += $"\n{UserManagement.GetServer(guildId).GetUser(userId).PrintAccounts()}";
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

            if (UserManagement.GetServer(_guildId).GetUser(_user.Id).SetMainAccount(message))
            {
                await UserExtensions.SendMessageAsync(_user, $"The account \"{message}\" is now your main account");
                Program.Conversations.Remove(_user.Username);
            }
            else
            {
                await UserExtensions.SendMessageAsync(_user, $"No Account named \"{message}\" found.\nPlease try again or type \"cancel\" to cancel the interaction.");
            }
        }
    }
}
