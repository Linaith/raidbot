﻿using Discord;
using Raidbot.Users;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Raidbot.Conversations
{
    class AccountRemoveConversation : IConversation
    {
        enum State { accountType, accountName }

        private readonly IUser _user;
        private readonly ulong _guildId;
        private State _state;
        private string _accountType;

        private AccountRemoveConversation(IUser user, ulong guildId)
        {
            _user = user;
            _guildId = guildId;
            if (UserManagement.GetServer(guildId).ListAccountTypes().Count == 1)
            {
                _state = State.accountName;
                _accountType = UserManagement.GetServer(guildId).ListAccountTypes().First();
            }
            else
            {
                _state = State.accountType;
            }
        }

        public static async Task<AccountRemoveConversation> Create(IUser user, ulong guildId)
        {
            AccountRemoveConversation conversation = new AccountRemoveConversation(user, guildId);
            await conversation.SendInitialMessage();
            return conversation;
        }

        private async Task SendInitialMessage()
        {
            switch (_state)
            {
                case State.accountType:
                    await UserExtensions.SendMessageAsync(_user, $"From which account type do you want to remove an account?\n{UserManagement.GetServer(_guildId).GetUser(_user.Id).PrintAccounts()}");
                    break;
                case State.accountName:
                    await UserExtensions.SendMessageAsync(_user, $"Which {_accountType} account do you want to remove?\n{UserManagement.GetServer(_guildId).GetUser(_user.Id).PrintAccounts()}");
                    break;
            }
        }

        public async void ProcessMessage(string message)
        {
            if (message.Equals("cancel", StringComparison.OrdinalIgnoreCase))
            {
                await UserExtensions.SendMessageAsync(_user, "interaction canceled");
                Program.Conversations.Remove(_user.Username);
                return;
            }

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
            if (UserManagement.GetServer(_guildId).ListAccountTypes().Contains(message))
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
            if (UserManagement.GetServer(_guildId).GetUser(_user.Id).RemoveAccount(_accountType, message))
            {
                await UserExtensions.SendMessageAsync(_user, $"Removed the account successfully.\nYour accounts are:\n{UserManagement.GetServer(_guildId).GetUser(_user.Id).PrintAccounts()}");
                Program.Conversations.Remove(_user.Username);
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
            foreach (string accountType in UserManagement.GetServer(_guildId).ListAccountTypes())
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