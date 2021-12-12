using Discord;
using Discord.WebSocket;
using Raidbot.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using static Raidbot.Models.RaidReminder;

namespace Raidbot.Services
{
    public class RaidService
    {
        private static readonly string _jsonFile = Path.Combine(Constants.SAVEFOLDER, "plannedRaids.json");
        private static Dictionary<string, Raid> Raids { get; set; }

        private readonly UserService _userService;

        private readonly ConversationService _conversationService;

        private readonly DiscordSocketClient _client;

        private readonly LogService _logService;

        private static readonly Random _random = new Random();

        public RaidService(UserService userService, ConversationService conversationService, DiscordSocketClient client, LogService logService)
        {
            Raids = new Dictionary<string, Raid>();
            LoadRaids();
            _userService = userService;
            _conversationService = conversationService;
            _client = client;
            _logService = logService;
        }

        public IEnumerable<Raid> ListRaids()
        {
            return Raids.Values;
        }

        public void AddRaid(Raid raid, ulong guildId, ulong channelId, ulong messageId)
        {
            raid.GuildId = guildId;
            raid.ChannelId = channelId;
            raid.MessageId = messageId;
            Raids.Add(raid.RaidId, raid);
            SaveRaids();
        }

        public async Task<bool> RemoveRaid(string raidId, SocketGuild guild)
        {
            if (guild != null && TryFindRaid(raidId, out Raid raid) && raid.GuildId.Equals(guild.Id))
            {
                IUserMessage userMessage = (IUserMessage)await guild.GetTextChannel(raid.ChannelId).GetMessageAsync(raid.MessageId);
                if (userMessage != null)
                {
                    await userMessage.DeleteAsync();
                }
                Raids.Remove(raidId);
                SaveRaids();
                return true;
            }
            return false;
        }

        public void SaveRaids()
        {
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(Raids);
            File.WriteAllText(_jsonFile, json);
        }

        private void LoadRaids()
        {
            if (File.Exists(_jsonFile))
            {
                string json = File.ReadAllText(_jsonFile);
                Raids = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Raid>>(json);
            }
        }

        public void RemoveUserFromAllRaids(IUser user)
        {
            foreach (var raid in Raids.Values)
            {
                RemoveUser(raid.RaidId, user.Id);
            }
            SaveRaids();
        }

        //TODO: eliminate this function!!!!
        public bool TryFindRaid(string raidId, out Raid raid)
        {
            if (Raids.ContainsKey(raidId))
            {
                raid = Raids[raidId];
                return true;
            }
            raid = null;
            return false;
        }

        public bool TryFindRaid(ulong GuildId, ulong ChannelId, ulong MessageId, out Raid raid)
        {
            foreach (var r in Raids)
            {
                if (r.Value.GuildId.Equals(GuildId) && r.Value.ChannelId.Equals(ChannelId) && r.Value.MessageId.Equals(MessageId))
                {
                    raid = r.Value;
                    return true;
                }
            }
            raid = null;
            return false;
        }

        public bool AddUser(string raidId, string userName, string role, Constants.Availability availability, out string resultMessage)
        {
            if (!Raids.ContainsKey(raidId))
            {
                resultMessage = "raid not found";
                return false;
            }
            Raid raid = Raids[raidId];
            User raidUser = new User(role, availability, userName, userName, raid.GetFreeUserId());
            return AddUser(raidId, raidUser, role, availability, out resultMessage);
        }

        public bool AddUser(string raidId, IGuildUser user, string role, Constants.Availability availability, string usedAccount, out string resultMessage)
        {
            string nickname = _userService.GetUserName(user.GuildId, user.Id);
            if (string.IsNullOrEmpty(nickname))
            {
                nickname = user.Nickname ?? user.Username;
            }
            User raidUser = new User(role, availability, nickname, usedAccount, user.Id);
            return AddUser(raidId, raidUser, role, availability, out resultMessage);
        }

        private bool AddUser(string raidId, User user, string role, Constants.Availability availability, out string resultMessage)
        {
            if (!Raids.ContainsKey(raidId))
            {
                resultMessage = "raid not found";
                return false;
            }
            Raid raid = Raids[raidId];
            if (!raid.CheckRoleAvailability(user.DiscordId, role, availability, out resultMessage))
            {
                return false;
            }

            if (availability.Equals(Constants.Availability.Flex))
            {
                raid.FlexRoles.Add(user);
            }
            else
            {
                raid.Users.Add(user.DiscordId, user);
            }
            SaveRaids();
            resultMessage = "Added to raid roster";
            return true;
        }

        public string RemoveUser(string raidId, string username)
        {
            if (Raids.ContainsKey(raidId))
            {
                Raid raid = Raids[raidId];
                foreach (User user in raid.Users.Values)
                {
                    if (user.Nickname == username && user.DiscordId < 256)
                    {
                        string message = RemoveUser(raid.RaidId, user.DiscordId);
                        SaveRaids();
                        return message;
                    }
                }
                return "user not found";
            }
            else
            {
                return "raid not found";
            }
        }

        public string RemoveUser(string raidId, ulong userId)
        {
            if (Raids.ContainsKey(raidId))
            {
                Raid raid = Raids[raidId];
                string message = "user not found";
                if (raid.Users.ContainsKey(userId))
                {
                    string name = raid.Users[userId].Nickname;
                    raid.Users.Remove(userId);
                    message = $"Successfully removed {name} from raid {raid.MessageId}";
                }
                raid.FlexRoles.RemoveAll(flexRole => flexRole.DiscordId.Equals(userId));
                SaveRaids();
                return message;
            }
            else
            {
                return "raid not found";
            }
        }

        public async Task HandleReaction(SocketReaction reaction, IGuildUser user, ulong guildId, string raidId)
        {
            if (!Raids.ContainsKey(raidId))
            {
                return;
            }

            Raid raid = Raids[raidId];
            IUserMessage userMessage = (IUserMessage)await reaction.Channel.GetMessageAsync(raid.MessageId);
            IEmote emote = reaction.Emote;

            if (emote.Equals(Constants.SignOffEmoji))
            {
                if (raid.Users.ContainsKey(user.Id))
                {
                    await _logService.LogRaid($"{raid.Users[user.Id].Nickname} signed off", raid);
                }
                else if (raid.FlexRoles.FindAll(x => x.DiscordId == user.Id).Count > 0)
                {
                    await _logService.LogRaid($"{raid.FlexRoles.Find(x => x.DiscordId == user.Id).Nickname} signed off", raid);
                }
                RemoveUser(raid.RaidId, user.Id);
                SaveRaids();
                await userMessage.ModifyAsync(msg => msg.Embed = raid.CreateRaidMessage());
                await userMessage.RemoveReactionAsync(reaction.Emote, user);
                return;
            }

            ulong userId = user.Id;
            if (_userService.GetAccounts(guildId, userId, raid.AccountType).Count == 0)
            {
                await UserExtensions.SendMessageAsync(user, $"No Account found, please add an Account with \"!user add {raid.AccountType} <AccountName>\".\n" +
                    "\n**This command only works on a server.**");
                return;
            }

            if (emote.Equals(Constants.FlexEmoji))
            {
                if (!_conversationService.UserHasConversation(user.Id))
                {
                    _conversationService.OpenSignUpConversation(this, reaction, user, raid, Constants.Availability.Flex);
                }
            }
            else if (raid.Users.ContainsKey(userId))
            {
                if (emote.Equals(Constants.SignOnEmoji))
                {
                    if (raid.IsAvailabilityChangeAllowed(userId, Constants.Availability.SignedUp))
                    {
                        raid.Users[userId].Availability = Constants.Availability.SignedUp;
                    }
                }
                else if (emote.Equals(Constants.UnsureEmoji))
                {
                    raid.Users[userId].Availability = Constants.Availability.Maybe;
                }
                else if (emote.Equals(Constants.BackupEmoji))
                {
                    raid.Users[userId].Availability = Constants.Availability.Backup;
                }
                await _logService.LogRaid($"{raid.Users[userId].Nickname} changed status to {raid.Users[userId].Availability}", raid);
            }
            else if (!_conversationService.UserHasConversation(user.Id))
            {
                if (emote.Equals(Constants.SignOnEmoji))
                {
                    _conversationService.OpenSignUpConversation(this, reaction, user, raid, Constants.Availability.SignedUp);
                }
                else if (emote.Equals(Constants.UnsureEmoji))
                {
                    _conversationService.OpenSignUpConversation(this, reaction, user, raid, Constants.Availability.Maybe);
                }
                else if (emote.Equals(Constants.BackupEmoji))
                {
                    _conversationService.OpenSignUpConversation(this, reaction, user, raid, Constants.Availability.Backup);
                }
            }
            SaveRaids();
            await userMessage.ModifyAsync(msg => msg.Embed = raid.CreateRaidMessage());
            await userMessage.RemoveReactionAsync(reaction.Emote, user);
        }

        public string CreateRaidId()
        {
            string raidId = _random.Next().ToString();
            if (Raids.ContainsKey(raidId))
            {
                return CreateRaidId();
            }
            return raidId;
        }

        public bool UpdateRaid(string raidId, Raid raid)
        {
            if (Raids.ContainsKey(raidId))
            {
                Raids[raidId] = raid;
                SaveRaids();
                return true;
            }
            return false;
        }

        public async Task ResetRaidAsync(string raidId, DateTime startTime, string[] text)
        {
            if (TryFindRaid(raidId, out Raid raid))
            {
                await SendMessageToEveryRaidMember(raid, text);
                raid.StartTime = startTime;
                raid.Reset();
                raid.MessageId = await RepostRaidMessage(raid);
                SaveRaids();
            }
        }

        public async Task<ulong> RepostRaidMessage(Raid raid)
        {
            SocketTextChannel channel = (SocketTextChannel)_client.GetChannel(raid.ChannelId);
            if (channel != null)
            {
                IUserMessage userMessage = (IUserMessage)await channel.GetMessageAsync(raid.MessageId);
                if (userMessage != null)
                {
                    await userMessage.DeleteAsync();
                }
            }
            return await PostRaidMessageAsync(channel, raid);
        }

        public async Task<ulong> PostRaidMessageAsync(ITextChannel channel, Raid raid)
        {
            if (channel != null)
            {
                var raidMessage = await channel.SendMessageAsync(embed: raid.CreateRaidMessage());
                await raidMessage.AddReactionAsync(Constants.SignOnEmoji);
                await raidMessage.AddReactionAsync(Constants.UnsureEmoji);
                await raidMessage.AddReactionAsync(Constants.BackupEmoji);
                await raidMessage.AddReactionAsync(Constants.FlexEmoji);
                await raidMessage.AddReactionAsync(Constants.SignOffEmoji);
                return raidMessage.Id;
            }
            else
            {
                throw new ArgumentNullException();
            }
        }

        public async Task SendMessageToEveryRaidMember(Raid raid, string[] text, string reason = "")
        {
            if (text.Length > 0)
            {
                string message = $"{raid.Title} {reason}: " + string.Join(' ', text);
                await SendMessageToEveryRaidMember(raid, message);
            }
        }

        public async Task SendMessageToEveryRaidMember(Raid raid, string message)
        {
            foreach (var user in raid.Users)
            {
                try
                {
                    SocketUser socketUser = _client.GetUser(user.Value.DiscordId);
                    if (socketUser != null)
                    {
                        await socketUser.SendMessageAsync(message);
                    }
                }
                catch { }
            }
        }

        public void AddReminder(Raid raid, ReminderType reminderType, double hoursBeforeRaid, string[] text, ulong channelId = 0)
        {
            AddReminder(raid, reminderType, hoursBeforeRaid, string.Join(' ', text), channelId);
        }

        public void AddReminder(Raid raid, ReminderType reminderType, double hoursBeforeRaid, string message, ulong channelId = 0)
        {
            RaidReminder reminder = new RaidReminder(reminderType, message, hoursBeforeRaid, channelId);
            raid.Reminders.Add(Guid.NewGuid(), reminder);
            SaveRaids();
        }

        public string ListReminders(Raid raid)
        {
            string message = "reminders:\n";
            foreach (var reminder in raid.Reminders)
            {
                message += $"Reminder type = {reminder.Value.Type}\n";
                if (reminder.Value.Type == ReminderType.Channel)
                {
                    SocketTextChannel channel = (SocketTextChannel)_client.GetChannel(reminder.Value.ChannelId);
                    message += $"   Channel = {channel.Name}\n";
                }
                message += $"   Hours before raid = {reminder.Value.HoursBeforeRaid}\n";
                message += $"   Message = {reminder.Value.Message}\n";
                message += $"   Reminder Id = {reminder.Key}\n";
            }
            return message;
        }

        public bool RemoveReminder(Raid raid, Guid reminderId)
        {
            bool result = raid.Reminders.Remove(reminderId);
            SaveRaids();
            return result;
        }
    }
}
