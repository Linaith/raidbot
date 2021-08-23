using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using static Raidbot.Constants;

namespace Raidbot.Models
{
    public class Raid
    {
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

        public Dictionary<Guid, RaidReminder> Reminders { get; set; }

        public Guid StandardReminderId { get; set; }

        public int Frequency { get; set; }

        //Roles that cout to the user cap
        private readonly List<Availability> blockingRole = new List<Availability> { Availability.SignedUp };

        public Raid(string raidId, int frequency)
        {
            Roles = new List<Role>();
            Users = new Dictionary<ulong, User>();
            FlexRoles = new List<User>();
            Reminders = new Dictionary<Guid, RaidReminder>();
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
                    resultMessage = $"max allowed flex roles are {MaxFlexRoles}";
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

        public bool IsAvailabilityChangeAllowed(ulong userId, Availability availability)
        {
            bool isCurrentlyRestrictedAvailability = Users[userId].Availability.Equals(Availability.SignedUp);
            bool IsChangeToRestrictedAvailability = availability.Equals(Availability.SignedUp);

            //user is signed up with yes, or there are free spots, or user doesn't want to change to yes
            return isCurrentlyRestrictedAvailability || GetFreeRoles().Contains(Users[userId].Role, StringComparer.OrdinalIgnoreCase) || !IsChangeToRestrictedAvailability;
        }

        private bool IsFlexAllowed(ulong userId)
        {
            return FlexRoles.FindAll(flexRole => flexRole.DiscordId.Equals(userId)).Count < MaxFlexRoles;
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
                string signedUpUsers = PrintUsers(role, Availability.SignedUp, ref noSignedUpUsers);
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
                //print if the user has the now processed role.
                if (role.Name.Equals(user.Value.Role, StringComparison.OrdinalIgnoreCase))
                {
                    if (availability.Equals(user.Value.Availability))
                    {
                        rolesString += $"\t{user.Value.Nickname} ({user.Value.UsedAccount}) {PrintAvailability(user.Value.Availability)}\n";
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
                if (user.Role.Equals(role.Name, StringComparison.OrdinalIgnoreCase))
                {
                    flexUsers += $"\t*{user.Nickname} - flex*\n";
                }
            }
            return flexUsers;
        }

        private string PrintAvailability(Availability availability)
        {
            if (availability.Equals(Availability.SignedUp))
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
            foreach (RaidReminder reminder in Reminders.Values)
            {
                reminder.Sent = false;
            }
        }

        public void AddStandardReminder()
        {
            double hoursBeforeRaid = 0.5;
            string message = $"The raid starts in {hoursBeforeRaid * 60} minutes.";
            RaidReminder reminder = new RaidReminder(RaidReminder.ReminderType.User, message, hoursBeforeRaid, 0);
            StandardReminderId = Guid.NewGuid();
            Reminders.Add(StandardReminderId, reminder);
        }
    }
}