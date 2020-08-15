using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Raidbot.Conversations;
using System;
using System.Threading.Tasks;

namespace Raidbot.Modules
{
    [RequireRole("Raidlead")]
    [RequireContext(ContextType.Guild)]
    [Group("raid")]
    public class RaidModule : ModuleBase<SocketCommandContext>
    {
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
                if (frequency == 0 && param.Equals("weekly", StringComparison.OrdinalIgnoreCase)) frequency = 7;
                if (frequency == 0 && param.Equals("daily", StringComparison.OrdinalIgnoreCase)) frequency = 1;
            }
            if (!Program.Conversations.ContainsKey(Context.User.Username))
            {
                if (text)
                {
                    Program.Conversations.Add(Context.User.Username, await RaidCreateContinuousTextConversation.Create(Context.User, Context.Guild, frequency));
                }
                else
                {
                    Program.Conversations.Add(Context.User.Username, await RaidCreateConversation.Create(Context.User, Context.Guild, frequency));
                }
            }
            await Context.Message.DeleteAsync();
        }

        [Command("delete")]
        [Summary("deletes a raid")]
        public async Task DeleteRaidAsync([Summary("The id of the raid")] string raidId)
        {
            await PlannedRaids.RemoveRaid(raidId, Context.Guild);
            await Context.Message.DeleteAsync();
        }

        [Command("end")]
        [Summary("ends a raid")]
        public async Task EndRaidAsync([Summary("The id of the raid")] string raidId, [Summary("The raid logs")] params string[] logs)
        {
            await DeleteRaidWithMessage(raidId, logs, "ended");
        }

        [Command("cancel")]
        [Summary("cancels a raid")]
        public async Task CancelRaidAsync([Summary("The id of the raid")] string raidId, [Summary("The raid logs")] params string[] text)
        {
            await DeleteRaidWithMessage(raidId, text, "was canceled");
        }

        public async Task DeleteRaidWithMessage(string raidId, string[] text, string reason)
        {
            if (PlannedRaids.TryFindRaid(raidId, out Raid raid))
            {
                await HelperFunctions.SendMessageToEveryRaidMember(raid, text, reason);
                await PlannedRaids.RemoveRaid(raid.RaidId, Context.Guild);
            }
            await Context.Message.DeleteAsync();
        }

        [Command("removeuser")]
        [Summary("removes a user from the raid")]
        public async Task RemoveUserAsync([Summary("The id of the raid")] string raidId, [Summary("The user")] IUser user)
        {
            if (PlannedRaids.TryFindRaid(raidId, out Raid raid))
            {
                await Context.Channel.SendMessageAsync(raid.RemoveUser(user));
                IUserMessage userMessage = (IUserMessage)await Context.Guild.GetTextChannel(raid.ChannelId).GetMessageAsync(raid.MessageId);
                await userMessage.ModifyAsync(msg => msg.Embed = raid.CreateRaidMessage());
            }
            await Context.Message.DeleteAsync();
        }

        [Command("adduser")]
        [Summary("adds a user to the raid")]
        public async Task AddUserAsync([Summary("The id of the raid")] string raidId, [Summary("The user")] IGuildUser user, [Summary("The role the user wants to play")] string role, [Summary("The account used to raid")] string usedAccount, [Summary("Availability of the user")] string availability = "")
        {
            if (PlannedRaids.TryFindRaid(raidId, out Raid raid))
            {
                Raid.Availability availabilityEnum = Raid.Availability.Yes;
                switch (availability.ToLower())
                {
                    case "maybe":
                        availabilityEnum = Raid.Availability.Maybe;
                        break;
                    case "backup":
                        availabilityEnum = Raid.Availability.Backup;
                        break;
                }
                raid.AddUser(user, role, availabilityEnum, usedAccount, out string resultMessage);
                await Context.Channel.SendMessageAsync(resultMessage);
                IUserMessage userMessage = (IUserMessage)await Context.Guild.GetTextChannel(raid.ChannelId).GetMessageAsync(raid.MessageId);
                await userMessage.ModifyAsync(msg => msg.Embed = raid.CreateRaidMessage());
            }
            await Context.Message.DeleteAsync();
        }

        [Command("addusers")]
        [Summary("adds a user to the raid")]
        public async Task AddUsersAsync([Summary("The id of the raid")] string raidId, params string[] users)
        {
            if (PlannedRaids.TryFindRaid(raidId, out Raid raid))
            {
                if (users.Length % 3 != 0)
                {
                    await Context.Channel.SendMessageAsync("wrong number of parameters.");
                }
                Raid.Availability availability = Raid.Availability.Yes;
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
                            raid.AddUser(guildUser, role, availability, usedAccount, out resultMessage);
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
        public async Task RemoveExternalUserAsync([Summary("The id of the raid")] string raidId, [Summary("The name of the user")] string userName)
        {
            if (PlannedRaids.TryFindRaid(raidId, out Raid raid))
            {
                await Context.Channel.SendMessageAsync(raid.RemoveUser(userName));
                IUserMessage userMessage = (IUserMessage)await Context.Guild.GetTextChannel(raid.ChannelId).GetMessageAsync(raid.MessageId);
                await userMessage.ModifyAsync(msg => msg.Embed = raid.CreateRaidMessage());
            }
            await Context.Message.DeleteAsync();
        }

        [Command("addexternaluser")]
        [Summary("adds a user to the raid")]
        public async Task AddExternalUserAsync([Summary("The id of the raid")] string raidId, [Summary("The name of the user")] string userName, [Summary("The role the user wants to play")] string role, [Summary("Availability of the user")] string availability = "")
        {
            if (PlannedRaids.TryFindRaid(raidId, out Raid raid))
            {
                Raid.Availability availabilityEnum = Raid.Availability.Yes;
                switch (availability.ToLower())
                {
                    case "maybe":
                        availabilityEnum = Raid.Availability.Maybe;
                        break;
                    case "backup":
                        availabilityEnum = Raid.Availability.Backup;
                        break;
                }
                raid.AddUser(userName, role, availabilityEnum, out string resultMessage);
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
            if (PlannedRaids.TryFindRaid(raidId, out Raid raid))
            {
                foreach (var channel in Context.Message.MentionedChannels)
                {
                    if (channel is ITextChannel textChannel)
                    {
                        IUserMessage userMessage = (IUserMessage)await Context.Guild.GetTextChannel(raid.ChannelId).GetMessageAsync(raid.MessageId);
                        await userMessage.DeleteAsync();
                        raid.MessageId = await HelperFunctions.PostRaidMessageAsync(textChannel, raid);
                        raid.ChannelId = channel.Id;
                        await Context.Channel.SendMessageAsync($"raid moved to {channel.Name}");
                        PlannedRaids.UpdateRaid(raidId, raid);
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
        public async Task ResetRaidAsync([Summary("The id of the raid")] string raidId, DateTime date, DateTime time, [Summary("message")] params string[] text)
        {
            if (PlannedRaids.TryFindRaid(raidId, out Raid raid))
            {
                await ResetRaidAsync(raid, date.Date + time.TimeOfDay, text);
            }
            await Context.Message.DeleteAsync();
        }


        [Command("reset")]
        [Summary("resets and rescedules a raid")]
        public async Task ResetRaidAsync([Summary("The id of the raid")] string raidId, [Summary("message")] params string[] text)
        {
            await ResetRaidAsync(raidId, 7, text);
        }

        [Command("reset")]
        [Summary("resets and rescedules a raid")]
        public async Task ResetRaidAsync([Summary("The id of the raid")] string raidId, int delay, [Summary("message")] params string[] text)
        {
            if (PlannedRaids.TryFindRaid(raidId, out Raid raid))
            {
                await ResetRaidAsync(raid, raid.StartTime.AddDays(delay), text);
            }
            await Context.Message.DeleteAsync();
        }

        public async Task ResetRaidAsync(Raid raid, DateTime startTime, string[] text)
        {
            await HelperFunctions.SendMessageToEveryRaidMember(raid, text);
            raid.Reset();
            raid.StartTime = startTime;
            raid.MessageId = await HelperFunctions.Instance().RepostRaidMessage(raid);
            PlannedRaids.UpdateRaid(raid.RaidId, raid);
        }

        [Group("edit")]
        public class RaidEdit : ModuleBase<SocketCommandContext>
        {
            // !raid edit
            [Command]
            public async Task DefaultEditAsync()
            {
                await ReplyAsync("Usage: !raid edit [what] [raidId]\n" +
                    "possible Edits: title, description, time, duration, organisator, guild, voicechat");
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
                if (PlannedRaids.TryFindRaid(raidId, out Raid raid))
                {
                    if (!Program.Conversations.ContainsKey(Context.User.Username))
                    {
                        IUserMessage userMessage = (IUserMessage)await Context.Guild.GetTextChannel(raid.ChannelId).GetMessageAsync(raid.MessageId);
                        Program.Conversations.Add(Context.User.Username, await RaidEditConversation.Create(Context.User, raidId, command, userMessage));
                    }
                }
                else
                {
                    await Context.Channel.SendMessageAsync($"raid {raidId} not found");
                }
            }
        }
    }
}
