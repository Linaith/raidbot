using Raidbot.Users.Accounts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Raidbot.Users
{
    public class DiscordServer
    {
        public List<string> UsableAccountTypes { get; set; }
        //UserId, User
        public Dictionary<ulong, User> Users { get; set; }

        ulong GuildId { get; }

        public DiscordServer(ulong guildId)
        {
            UsableAccountTypes = new List<string>();
            Users = new Dictionary<ulong, User>();
            GuildId = guildId;
        }

        public void AddAccountType(string accountType)
        {
            if (!UsableAccountTypes.Contains(accountType))
            {
                UsableAccountTypes.Add(accountType);
                UserManagement.SaveUsers();
            }
        }

        public bool RemoveAccountType(string accountType)
        {
            if (UsableAccountTypes.Contains(accountType))
            {
                UsableAccountTypes.Remove(accountType);
                UserManagement.SaveUsers();
                return true;
            }
            return false;
        }

        public List<string> ListAccountTypes()
        {
            return UsableAccountTypes;
        }

        public void RemoveUser(ulong userId)
        {
            Users.Remove(userId);
            UserManagement.SaveUsers();
        }

        public User GetUser(ulong userId)
        {
            if (!Users.ContainsKey(userId))
            {
                User newUser = new User(GuildId);
                Users.Add(userId, newUser);
                UserManagement.SaveUsers();
            }
            return Users[userId];
        }
    }
}
