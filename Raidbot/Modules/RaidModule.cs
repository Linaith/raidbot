﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Raidbot.Conversations;
using Raidbot.Models;
using Raidbot.Services;
using System;
using System.Threading.Tasks;
using static Raidbot.Models.RaidReminder;

namespace Raidbot.Modules
{
    [RequireRole("Raidlead")]
    [RequireContext(ContextType.Guild)]
    [Group("raid")]
    public class RaidModule : ModuleBase<SocketCommandContext>
    {
        private readonly ConversationService _conversationService;
        private readonly RaidService _raidService;

        public RaidModule(ConversationService conversationService, RaidService raidService)
        {
            _conversationService = conversationService;
            _raidService = raidService;
        }

        [Command]
        //[Command("help")]
        [Summary("explains raid commands")]
        public async Task RaidHelpAsync()
        {
            string helpMessage = "existing raid commands:\n" +
                "!raid create [text] [weekly|daily]\n" +
                "!raid delete <RaidId>\n" +
                "!raid end <RaidId> [Logs or message]\n" +
                "!raid cancel <RaidId> [message]\n" +
                "!raid message <RaidId> [message]\n" +
                "!raid removeuser <RaidId> <@UserName>\n" +
                "!raid adduser <RaidId> <@UserName> <role> <usedAccount> [maybe/backup]\n" +
                "!raid addusers <RaidId> <@UserName> <role> <usedAccount> [<@UserName> <role> <usedAccount>]\n" +
                "!raid removeexternaluser <RaidId> <UserName>\n" +
                "!raid addexternaluser <RaidId> <UserName> <role> [maybe/backup]\n" +
                "!raid move <RaidId> #channel\n" +
                "!raid reset <RaidId>\n" +
                "!raid reset <RaidId> <delay in days>\n" +
                "!raid reset <RaidId> <date: dd.mm.yyyy hh:mm>\n" +
                "!raid edit";
            await ReplyAsync(helpMessage);
        }

        [Command("create")]
        [Summary("creates a raid")]
        public async Task CreateRaidAsync([Summary("message")] params string[] parameters)
        {
            int frequency = 0;
            bool text = false;
            foreach (string param in parameters)
            {
                if (param.Equals("text", StringComparison.OrdinalIgnoreCase)) text = true;
                if (frequency == 0 && param.Equals("biweekly", StringComparison.OrdinalIgnoreCase)) frequency = 14;
                if (frequency == 0 && param.Equals("weekly", StringComparison.OrdinalIgnoreCase)) frequency = 7;
                if (frequency == 0 && param.Equals("daily", StringComparison.OrdinalIgnoreCase)) frequency = 1;
            }
            if (!_conversationService.UserHasConversation(Context.User.Id))
            {
                if (text)
                {
                    _conversationService.OpenRaidCreateContinuousTextConversation(_raidService, Context.User, Context.Guild, frequency);
                }
                else
                {
                    _conversationService.OpenRaidCreateConversation(_raidService, Context.User, Context.Guild, frequency);
                }
            }
            await Context.Message.DeleteAsync();
        }

        [Command("delete")]
        [Summary("deletes a raid")]
        public async Task DeleteRaidAsync([Summary("Id of the raid")] string raidId)
        {
            await _raidService.RemoveRaid(raidId, Context.Guild);
            await Context.Message.DeleteAsync();
        }

        [Command("end")]
        [Summary("ends a raid")]
        public async Task EndRaidAsync([Summary("Id of the raid")] string raidId, [Summary("Message")] params string[] text)
        {
            await DeleteRaidWithMessage(raidId, text, "ended");
        }

        [Command("cancel")]
        [Summary("cancels a raid")]
        public async Task CancelRaidAsync([Summary("Id of the raid")] string raidId, [Summary("Message")] params string[] text)
        {
            await DeleteRaidWithMessage(raidId, text, "was canceled");
        }

        public async Task DeleteRaidWithMessage(string raidId, string[] text, string reason)
        {
            if (_raidService.TryFindRaid(raidId, out Raid raid))
            {
                await _raidService.SendMessageToEveryRaidMember(raid, text, reason);
                await _raidService.RemoveRaid(raid.RaidId, Context.Guild);
            }
            await Context.Message.DeleteAsync();
        }

        [Command("message")]
        [Summary("send message to raid members")]
        public async Task SendMessageAsync([Summary("Id of the raid")] string raidId, [Summary("Message")] params string[] text)
        {
            if (_raidService.TryFindRaid(raidId, out Raid raid))
            {
                await _raidService.SendMessageToEveryRaidMember(raid, text);
            }
        }

        [Command("removeuser")]
        [Summary("removes a user from the raid")]
        public async Task RemoveUserAsync([Summary("Id of the raid")] string raidId, [Summary("The user")] IUser user)
        {
            if (_raidService.TryFindRaid(raidId, out Raid raid))
            {
                await Context.Channel.SendMessageAsync(_raidService.RemoveUser(raidId, user.Id));
                IUserMessage userMessage = (IUserMessage)await Context.Guild.GetTextChannel(raid.ChannelId).GetMessageAsync(raid.MessageId);
                await userMessage.ModifyAsync(msg => msg.Embed = raid.CreateRaidMessage());
            }
            await Context.Message.DeleteAsync();
        }

        [Command("adduser")]
        [Summary("adds a user to the raid")]
        public async Task AddUserAsync([Summary("Id of the raid")] string raidId, [Summary("The user")] IGuildUser user, [Summary("The role the user wants to play")] string role, [Summary("The account used to raid")] string usedAccount, [Summary("Availability of the user")] string availability = "")
        {
            if (_raidService.TryFindRaid(raidId, out Raid raid))
            {
                Constants.Availability availabilityEnum = Constants.Availability.SignedUp;
                switch (availability.ToLower())
                {
                    case "maybe":
                        availabilityEnum = Constants.Availability.Maybe;
                        break;
                    case "backup":
                        availabilityEnum = Constants.Availability.Backup;
                        break;
                }
                _raidService.AddUser(raidId, user, role, availabilityEnum, usedAccount, out string resultMessage);
                await Context.Channel.SendMessageAsync(resultMessage);
                IUserMessage userMessage = (IUserMessage)await Context.Guild.GetTextChannel(raid.ChannelId).GetMessageAsync(raid.MessageId);
                await userMessage.ModifyAsync(msg => msg.Embed = raid.CreateRaidMessage());
            }
            await Context.Message.DeleteAsync();
        }

        [Command("addusers")]
        [Summary("adds a user to the raid")]
        public async Task AddUsersAsync([Summary("Id of the raid")] string raidId, params string[] users)
        {
            if (_raidService.TryFindRaid(raidId, out Raid raid))
            {
                if (users.Length % 3 != 0)
                {
                    await Context.Channel.SendMessageAsync("wrong number of parameters.");
                }
                Constants.Availability availability = Constants.Availability.SignedUp;
                string resultMessage = string.Empty;
                for (int i = 0; i < users.Length; i += 3)
                {
                    string userName = users[i];
                    string role = users[i + 1];
                    string usedAccount = users[i + 2];

                    foreach (SocketUser user in Context.Message.MentionedUsers)
                    {
                        if (user.Mention == userName && user is IGuildUser guildUser)
                        {
                            _raidService.AddUser(raidId, guildUser, role, availability, usedAccount, out resultMessage);
                        }

                    }
                }
                await Context.Channel.SendMessageAsync(resultMessage);
                IUserMessage userMessage = (IUserMessage)await Context.Guild.GetTextChannel(raid.ChannelId).GetMessageAsync(raid.MessageId);
                await userMessage.ModifyAsync(msg => msg.Embed = raid.CreateRaidMessage());
            }
            await Context.Message.DeleteAsync();
        }

        [Command("removeexternaluser")]
        [Summary("removes a user from the raid")]
        public async Task RemoveExternalUserAsync([Summary("Id of the raid")] string raidId, [Summary("The name of the user")] string userName)
        {
            if (_raidService.TryFindRaid(raidId, out Raid raid))
            {
                await Context.Channel.SendMessageAsync(_raidService.RemoveUser(raidId, userName));
                IUserMessage userMessage = (IUserMessage)await Context.Guild.GetTextChannel(raid.ChannelId).GetMessageAsync(raid.MessageId);
                await userMessage.ModifyAsync(msg => msg.Embed = raid.CreateRaidMessage());
            }
            await Context.Message.DeleteAsync();
        }

        [Command("addexternaluser")]
        [Summary("adds a user to the raid")]
        public async Task AddExternalUserAsync([Summary("Id of the raid")] string raidId, [Summary("The name of the user")] string userName, [Summary("The role the user wants to play")] string role, [Summary("Availability of the user")] string availability = "")
        {
            if (_raidService.TryFindRaid(raidId, out Raid raid))
            {
                Constants.Availability availabilityEnum = Constants.Availability.SignedUp;
                switch (availability.ToLower())
                {
                    case "maybe":
                        availabilityEnum = Constants.Availability.Maybe;
                        break;
                    case "backup":
                        availabilityEnum = Constants.Availability.Backup;
                        break;
                }
                _raidService.AddUser(raidId, userName, role, availabilityEnum, out string resultMessage);
                await Context.Channel.SendMessageAsync(resultMessage);
                IUserMessage userMessage = (IUserMessage)await Context.Guild.GetTextChannel(raid.ChannelId).GetMessageAsync(raid.MessageId);
                await userMessage.ModifyAsync(msg => msg.Embed = raid.CreateRaidMessage());
            }
            await Context.Message.DeleteAsync();
        }

        [Command("move")]
        //channelName is needed for correct command routing
        public async Task EditChannelAsync(string raidId, string channelName)
        {
            if (_raidService.TryFindRaid(raidId, out Raid raid))
            {
                foreach (var channel in Context.Message.MentionedChannels)
                {
                    if (channel is ITextChannel textChannel)
                    {
                        IUserMessage userMessage = (IUserMessage)await Context.Guild.GetTextChannel(raid.ChannelId).GetMessageAsync(raid.MessageId);
                        raid.MessageId = await _raidService.PostRaidMessageAsync(textChannel, raid);
                        raid.ChannelId = channel.Id;
                        await userMessage.DeleteAsync();
                        await Context.Channel.SendMessageAsync($"raid moved to {channel.Name}");
                        _raidService.UpdateRaid(raidId, raid);
                        return;
                    }
                }
                await Context.Channel.SendMessageAsync($"channel not found.");
            }
            else
            {
                await Context.Channel.SendMessageAsync("raid not found.");
            }
        }

        [Command("reset")]
        [Summary("resets and rescedules a raid")]
        public async Task ResetRaidAsync([Summary("Id of the raid")] string raidId, DateTime date, DateTime time, [Summary("message")] params string[] text)
        {
            await _raidService.ResetRaidAsync(raidId, date.Date + time.TimeOfDay, text);
            await Context.Message.DeleteAsync();
        }


        [Command("reset")]
        [Summary("resets and rescedules a raid")]
        public async Task ResetRaidAsync([Summary("Id of the raid")] string raidId, [Summary("message")] params string[] text)
        {
            await ResetRaidAsync(raidId, 7, text);
        }

        [Command("reset")]
        [Summary("resets and rescedules a raid")]
        public async Task ResetRaidAsync([Summary("Id of the raid")] string raidId, int delay, [Summary("message")] params string[] text)
        {
            if (_raidService.TryFindRaid(raidId, out Raid raid))
            {
                await _raidService.ResetRaidAsync(raidId, raid.StartTime.AddDays(delay), text);
            }
            await Context.Message.DeleteAsync();
        }

        [Group("edit")]
        public class RaidEdit : ModuleBase<SocketCommandContext>
        {
            private readonly ConversationService _conversationService;
            private readonly RaidService _raidService;

            public RaidEdit(ConversationService conversationService, RaidService raidService)
            {
                _conversationService = conversationService;
                _raidService = raidService;
            }

            // !raid edit
            [Command]
            public async Task DefaultEditAsync()
            {
                await ReplyAsync("Usage: !raid edit [what] [raidId]\n" +
                    "possible Edits: title, description, time, duration, organisator, guild, voicechat, role");
            }

            // !raid edit title 12345678900
            [Command("title")]
            public async Task EditTitleAsync(string raidId)
            {
                await EditRaid(raidId, RaidEditConversation.Edits.Title);
                await Context.Message.DeleteAsync();
            }

            // !raid edit description 12345678900
            [Command("description")]
            public async Task EditDescriptionAsync(string raidId)
            {
                await EditRaid(raidId, RaidEditConversation.Edits.Description);
                await Context.Message.DeleteAsync();
            }

            // !raid edit time 12345678900
            [Command("time")]
            public async Task EditTimeAsync(string raidId)
            {
                await EditRaid(raidId, RaidEditConversation.Edits.Time);
                await Context.Message.DeleteAsync();
            }

            // !raid edit duration 12345678900
            [Command("duration")]
            public async Task EditDurationAsync(string raidId)
            {
                await EditRaid(raidId, RaidEditConversation.Edits.Duation);
                await Context.Message.DeleteAsync();
            }

            // !raid edit duration 12345678900
            [Command("organisator")]
            public async Task EditOrganisatorAsync(string raidId)
            {
                await EditRaid(raidId, RaidEditConversation.Edits.Organisator);
                await Context.Message.DeleteAsync();
            }

            // !raid edit duration 12345678900
            [Command("guild")]
            public async Task EditGuildAsync(string raidId)
            {
                await EditRaid(raidId, RaidEditConversation.Edits.Guild);
                await Context.Message.DeleteAsync();
            }

            // !raid edit duration 12345678900
            [Command("voicechat")]
            public async Task EditVoiceChatAsync(string raidId)
            {
                await EditRaid(raidId, RaidEditConversation.Edits.VoiceChat);
                await Context.Message.DeleteAsync();
            }

            private async Task EditRaid(string raidId, RaidEditConversation.Edits command)
            {
                if (_raidService.TryFindRaid(raidId, out Raid raid))
                {
                    if (!_conversationService.UserHasConversation(Context.User.Id))
                    {
                        IUserMessage userMessage = (IUserMessage)await Context.Guild.GetTextChannel(raid.ChannelId).GetMessageAsync(raid.MessageId);
                        _conversationService.OpenRaidEditConversation(_raidService, Context.User, raidId, command, userMessage);
                    }
                }
                else
                {
                    await Context.Channel.SendMessageAsync($"raid {raidId} not found");
                }
            }

            // !raid edit role 12345678900
            [Command("role")]
            public async Task EditRoleAsync(string raidId)
            {
                if (_raidService.TryFindRaid(raidId, out Raid raid))
                {
                    if (!_conversationService.UserHasConversation(Context.User.Id))
                    {
                        IUserMessage userMessage = (IUserMessage)await Context.Guild.GetTextChannel(raid.ChannelId).GetMessageAsync(raid.MessageId);
                        _conversationService.OpenRaidEditRoleConversation(_raidService, Context.User, raidId, userMessage);
                    }
                }
                else
                {
                    await Context.Channel.SendMessageAsync($"raid {raidId} not found");
                }
                await Context.Message.DeleteAsync();
            }

        }

        [Group("reminder")]
        public class RaidReminder : ModuleBase<SocketCommandContext>
        {
            private readonly RaidService _raidService;

            public RaidReminder(RaidService raidService)
            {
                _raidService = raidService;
            }

            // !raid edit
            [Command]
            public async Task DefaultReminderAsync()
            {
                await ReplyAsync("existing raid reminder commands:\n" +
                "!raid reminder add <RaidId> <HoursBeforeRaid> <Message>\n" +
                "!raid reminder add <RaidId> <#Channel> <HoursBeforeRaid> <Message>\n" +
                "!raid reminder list <RaidId>\n" +
                "!raid reminder remove <RaidId> <ReminderId>");
            }

            // !raid reminder add 12345678900 24 This is a reminder
            [Command("add")]
            public async Task AddReminderAsync([Summary("Id of the raid")] string raidId, double hoursBeforeRaid, [Summary("Message")] params string[] text)
            {
                if (_raidService.TryFindRaid(raidId, out Raid raid))
                {
                    _raidService.AddReminder(raid, ReminderType.User, hoursBeforeRaid, text);
                    await Context.Channel.SendMessageAsync($"reminder added");
                    return;
                }
                else
                {
                    await Context.Channel.SendMessageAsync("raid not found.");
                }
                await Context.Message.DeleteAsync();
            }

            // !raid reminder add 12345678900 #Channel 24 This is a reminder
            [Command("add")]
            public async Task AddReminderAsync([Summary("Id of the raid")] string raidId, string channelName, double hoursBeforeRaid, [Summary("Message")] params string[] text)
            {
                if (_raidService.TryFindRaid(raidId, out Raid raid))
                {
                    foreach (SocketGuildChannel channel in Context.Message.MentionedChannels)
                    {
                        if (channel is ITextChannel)
                        {
                            _raidService.AddReminder(raid, ReminderType.Channel, hoursBeforeRaid, text, channel.Id);
                            await Context.Channel.SendMessageAsync($"reminder added");
                            return;
                        }
                    }
                    await Context.Channel.SendMessageAsync($"channel not found.");
                }
                else
                {
                    await Context.Channel.SendMessageAsync("raid not found.");
                }
                await Context.Message.DeleteAsync();
            }

            // !raid reminder list 12345678900
            [Command("list")]
            public async Task ListRemindersAsync(string raidId)
            {
                if (_raidService.TryFindRaid(raidId, out Raid raid))
                {
                    await Context.Channel.SendMessageAsync(_raidService.ListReminders(raid));
                }
                else
                {
                    await Context.Channel.SendMessageAsync("raid not found.");
                }
                await Context.Message.DeleteAsync();
            }

            // !raid reminder remove 12345678900 298bb4ed-fc0a-4961-be70-b1c7f4490229
            [Command("remove")]
            public async Task RemoveReminderAsync(string raidId, string reminderId)
            {
                if (_raidService.TryFindRaid(raidId, out Raid raid))
                {
                    if (Guid.TryParse(reminderId, out Guid id))
                    {
                        if (_raidService.RemoveReminder(raid, id))
                        {
                            await Context.Channel.SendMessageAsync("reminder removed.");
                        }
                        else
                        {
                            await Context.Channel.SendMessageAsync("reminder not found.");
                        }
                    }
                    else
                    {
                        await Context.Channel.SendMessageAsync("invalid reminder Id.");
                    }
                }
                else
                {
                    await Context.Channel.SendMessageAsync("raid not found.");
                }
                await Context.Message.DeleteAsync();
            }

            // !raid reminder repair
            [Command("repair")]
            public async Task RepairRemindersAsync()
            {
                foreach (Raid raid in _raidService.ListRaids())
                {
                    if (raid.StandardReminderId == Guid.Empty)
                    {
                        raid.AddStandardReminder();
                    }
                }
                await Context.Channel.SendMessageAsync("added standard reminders to raids.");
                await Context.Message.DeleteAsync();
            }
        }
    }
}
