using Discord;
using Discord.WebSocket;
using Raidbot.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Raidbot
{
    public class Raid
    {
        public enum Availability { Yes, Maybe, Backup, Flex };
        private const int maxFlexRoles = 2;

        public class User
        {
            public User(string role, Availability availability, string nickname, string usedAccount, ulong userid)
            {
                Role = role;
                Availability = availability;
                DiscordId = userid;
                UsedAccount = usedAccount;
                Nickname = nickname;
            }

            public string Role { get; set; }
            public Availability Availability { get; set; }
            public string Nickname { get; set; }
            public string UsedAccount { get; set; }
            public ulong DiscordId { get; set; }
        }

        public class Role
        {
            public Role(int spots, string name, string description = "")
            {
                Name = name;
                Spots = spots;
                Description = description;
            }

            public string Name { get; }
            public int Spots { get; }
            public string Description { get; }
        }

        public string AccountType { get; set; } = "GuildWars2";

        public string Title { get; set; }

        public string Description { get; set; }

        public DateTime StartTime { get; set; }

        public double RaidDuration { get; set; }

        public string Organisator { get; set; }

        public string Guild { get; set; }

        public string VoiceChat { get; set; }

        //role name, number of spots
        public List<Role> Roles { get; set; }

        //user, role
        public Dictionary<ulong, User> Users { get; set; }

        //FlexRoles
        public readonly List<User> FlexRoles;

        //used to close raids
        public string RaidId { get; private set; }

        //used to edit the message
        public ulong MessageId { get; set; }

        public ulong ChannelId { get; set; }

        public ulong GuildId { get; set; }

        public bool ReminderSent { get; set; } = false;

        public int Frequency { get; set; }

        //Roles that cout to the user cap
        private readonly List<Availability> blockingRole = new List<Availability> { Availability.Yes };

        public Raid(string raidId, int frequency)
        {
            Roles = new List<Role>();
            Users = new Dictionary<ulong, User>();
            FlexRoles = new List<User>();
            RaidId = raidId;
            Frequency = frequency;
        }

        public List<string> GetFreeRoles()
        {
            var freeRoles = new List<string>();
            foreach (Role role in Roles)
            {
                int counter = 0;
                foreach (var user in Users)
                {
                    if (role.Name.Equals(user.Value.Role, StringComparison.OrdinalIgnoreCase) && blockingRole.Contains(user.Value.Availability)) counter++;
                }
                if (counter < role.Spots) freeRoles.Add(role.Name);
            }
            return freeRoles;
        }

        public bool IsSignedUp(IUser user)
        {
            return Users.ContainsKey(user.Id);
        }

        public bool AddUser(string userName, string role, Availability availability, out string resultMessage)
        {
            User raidUser = new User(role, availability, userName, userName, GetFreeUserId());
            return AddUser(raidUser, role, availability, out resultMessage);
        }

        public ulong GetFreeUserId(ushort startValue = 0)
        {
            ushort id = startValue;
            foreach (User user in Users.Values)
            {
                if (user.DiscordId == id)
                {
                    id += 1;
                    return GetFreeUserId(id);
                }
            }
            return id;
        }

        public bool AddUser(IGuildUser user, string role, Availability availability, string usedAccount, out string resultMessage)
        {
            string nickname = user.Nickname ?? user.Username;
            User raidUser = new User(role, availability, nickname, usedAccount, user.Id);
            return AddUser(raidUser, role, availability, out resultMessage);
        }

        private bool AddUser(User user, string role, Availability availability, out string resultMessage)
        {
            if (!CheckRoleAvailability(user.DiscordId, role, availability, out resultMessage))
            {
                return false;
            }

            if (availability.Equals(Availability.Flex))
            {
                FlexRoles.Add(user);
            }
            else
            {
                Users.Add(user.DiscordId, user);
            }

            PlannedRaids.UpdateRaid(RaidId, this);
            resultMessage = "Added to raid roster";
            return true;
        }

        public bool CheckRoleAvailability(ulong userId, string role, Availability availability, out string resultMessage)
        {
            resultMessage = string.Empty;
            if (!RoleExists(role))
            {
                resultMessage = $"No role named {role} found.";
                return false;
            }

            if (availability.Equals(Availability.Flex))
            {
                if (!IsFlexAllowed(userId))
                {
                    resultMessage = $"max allowed flex roles are {maxFlexRoles}";
                    return false;
                }
            }
            else
            {
                if (Users.ContainsKey(userId))
                {
                    resultMessage = $"user {Users[userId].Nickname} is already signed up";
                    return false;
                }
                if (!GetFreeRoles().Contains(role, StringComparer.OrdinalIgnoreCase))
                {
                    resultMessage = $"no free role named {role} found";
                    return false;
                }
            }
            return true;
        }

        public async Task ManageUser(SocketReaction reaction, IEmote emote, ulong guildId)
        {
            ulong userId = reaction.User.Value.Id;
            if (UserManagement.GetServer(guildId).GetUser(reaction.UserId).GetAccounts(AccountType).Count() == 0)
            {
                await UserExtensions.SendMessageAsync(reaction.User.Value, $"No Account found, please add an Account with \"!user add {AccountType} <AccountName>\".\n" +
                    "\n**This command only works on a server.**");
                return;
            }

            if (emote.Equals(Constants.FlexEmoji))
            {
                if (!Program.Conversations.ContainsKey(reaction.User.Value.Username))
                {
                    Program.Conversations.Add(reaction.User.Value.Username, await SignUpConversation.Create(reaction, this, Availability.Flex));
                }
            }
            else if (Users.ContainsKey(userId))
            {
                if (emote.Equals(Constants.SignOnEmoji))
                {
                    if (IsAvailabilityChangeAllowed(userId, Availability.Yes))
                    {
                        Users[userId].Availability = Availability.Yes;
                    }
                }
                else if (emote.Equals(Constants.UnsureEmoji))
                {
                    Users[userId].Availability = Availability.Maybe;
                }
                else if (emote.Equals(Constants.BackupEmoji))
                {
                    Users[userId].Availability = Availability.Backup;
                }
                else if (emote.Equals(Constants.SignOffEmoji))
                {
                    RemoveUser(reaction.User.Value);
                }
            }
            else if (!Program.Conversations.ContainsKey(reaction.User.Value.Username))
            {
                if (emote.Equals(Constants.SignOnEmoji))
                {
                    Program.Conversations.Add(reaction.User.Value.Username, await SignUpConversation.Create(reaction, this, Availability.Yes));
                }
                else if (emote.Equals(Constants.UnsureEmoji))
                {
                    Program.Conversations.Add(reaction.User.Value.Username, await SignUpConversation.Create(reaction, this, Availability.Maybe));
                }
                else if (emote.Equals(Constants.BackupEmoji))
                {
                    Program.Conversations.Add(reaction.User.Value.Username, await SignUpConversation.Create(reaction, this, Availability.Backup));
                }
            }
            PlannedRaids.UpdateRaid(RaidId, this);
            IUserMessage userMessage = (IUserMessage)await reaction.Channel.GetMessageAsync(MessageId);
            await userMessage.ModifyAsync(msg => msg.Embed = CreateRaidMessage());
            await userMessage.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
        }

        private bool IsAvailabilityChangeAllowed(ulong userId, Availability availability)
        {
            bool isCurrentlyRestrictedAvailability = Users[userId].Availability.Equals(Availability.Yes);
            bool IsChangeToRestrictedAvailability = availability.Equals(Availability.Yes);

            //user is signed up with yes, or there are free spots, or user doesn't want to change to yes
            return isCurrentlyRestrictedAvailability || GetFreeRoles().Contains(Users[userId].Role, StringComparer.OrdinalIgnoreCase) || !IsChangeToRestrictedAvailability;
        }

        private bool IsFlexAllowed(ulong userId)
        {
            return FlexRoles.FindAll(flexRole => flexRole.DiscordId.Equals(userId)).Count < maxFlexRoles;
        }

        public string RemoveUser(IUser user)
        {
            return RemoveUser(user.Id);
        }

        public string RemoveUser(string username)
        {
            foreach (User user in Users.Values)
            {
                if (user.Nickname == username && user.DiscordId < 256)
                {
                    return RemoveUser(user.DiscordId);
                }
            }
            return "user not found";
        }

        private string RemoveUser(ulong userId)
        {
            string message = "user not found";
            if (Users.ContainsKey(userId))
            {
                string name = Users[userId].Nickname;
                Users.Remove(userId);
                PlannedRaids.UpdateRaid(RaidId, this);
                message = $"Successfully removed {name} from raid {MessageId}";
            }
            FlexRoles.RemoveAll(flexRole => flexRole.DiscordId.Equals(userId));
            return message;
        }

        public Embed CreateRaidMessage()
        {
            var embed = new EmbedBuilder()
            {
                Title = Title,
                Description = Description,
                Footer = new EmbedFooterBuilder()
                {
                    Text = $"RaidId: {RaidId}"
                }
            };
            AddMessageDetails(ref embed);
            AddMessageRoles(ref embed);


            return embed.Build();
        }

        private void AddMessageDetails(ref EmbedBuilder embed)
        {
            embed.AddField("Date", $"{StartTime.ToLongDateString()}");
            embed.AddField("Time", $"from: {StartTime.ToShortTimeString()}  to: {StartTime.AddHours(RaidDuration).ToShortTimeString()}");
            embed.AddField("Organisator", Organisator, true);
            embed.AddField("Guild", Guild, true);
            embed.AddField("Voice chat", VoiceChat, true);
        }

        private void AddMessageRoles(ref EmbedBuilder embed)
        {
            Dictionary<string, string> fieldList = new Dictionary<string, string>();
            int signedUpUsersTotal = 0;
            int maxUsers = 0;

            foreach (Role role in Roles)
            {
                int noSignedUpUsers = 0;
                //print signed up users
                string signedUpUsers = PrintUsers(role, Availability.Yes, ref noSignedUpUsers);
                signedUpUsers += PrintUsers(role, Availability.Maybe, ref noSignedUpUsers);
                signedUpUsers += PrintUsers(role, Availability.Backup, ref noSignedUpUsers);
                signedUpUsers += PrintFlexUsers(role);

                if (string.IsNullOrEmpty(signedUpUsers)) signedUpUsers = "-";
                fieldList.Add($"{role.Name}: {role.Description} ({noSignedUpUsers}/{role.Spots})", $"{signedUpUsers}");

                signedUpUsersTotal += noSignedUpUsers;
                maxUsers += role.Spots;
            }
            //rolesString += $"{PrintSignedOffUsers()}";

            embed.AddField("Signed up", $"({ signedUpUsersTotal}/{ maxUsers}):");
            foreach (var field in fieldList)
            {
                embed.AddField(field.Key, field.Value);
            }
        }

        private string PrintUsers(Role role, Availability availability, ref int signedUpUsers)
        {
            string rolesString = string.Empty;
            foreach (var user in Users)
            {
                string name = UserManagement.GetServer(GuildId).GetUser(user.Value.DiscordId).Name;
                if (string.IsNullOrEmpty(name))
                {
                    name = user.Value.Nickname;
                }
                //print if the user has the now processed role.
                if (role.Name.Equals(user.Value.Role, StringComparison.OrdinalIgnoreCase))
                {
                    if (availability.Equals(user.Value.Availability))
                    {
                        rolesString += $"\t{name} ({user.Value.UsedAccount}) {PrintAvailability(user.Value.Availability)}\n";
                        if (blockingRole.Contains(user.Value.Availability))
                        {
                            signedUpUsers++;
                        }
                    }
                }
            }
            return rolesString;
        }

        private string PrintFlexUsers(Role role)
        {
            string flexUsers = string.Empty;
            foreach (User user in FlexRoles)
            {
                string name = UserManagement.GetServer(GuildId).GetUser(user.DiscordId).Name;
                if (string.IsNullOrEmpty(name))
                {
                    name = user.Nickname;
                }
                if (user.Role.Equals(role.Name, StringComparison.OrdinalIgnoreCase))
                {
                    flexUsers += $"\t*{name} - flex*\n";
                }
            }
            return flexUsers;
        }

        private string PrintAvailability(Availability availability)
        {
            if (availability.Equals(Availability.Yes))
            {
                return String.Empty;
            }
            return $" - {availability}";
        }

        public bool RoleExists(string role)
        {
            foreach (Role r in Roles)
            {
                if (r.Name.Equals(role.Trim(), StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        public bool AddRole(int spots, string name, string description = "")
        {
            if (!RoleExists(name))
            {
                Roles.Add(new Role(spots, name, description));
                return true;
            }
            return false;
        }

        public void Reset()
        {
            Users.Clear();
            FlexRoles.Clear();
        }
    }
}
