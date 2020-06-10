using Discord;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Raidbot.Conversations
{
    public class RaidCreateContinuousTextConversation : IConversation
    {
        enum State { creation, acception }

        private readonly IUser _user;

        private ITextChannel _channel;

        private readonly Raid _raid;

        private readonly IGuild _guild;

        private State _state;

        private RaidCreateContinuousTextConversation(IUser user, IGuild guild, int frequency)
        {
            _user = user;
            _raid = new Raid(PlannedRaids.CreateRaidId(), frequency);
            _guild = guild;
            _state = State.creation;
        }

        public static async Task<RaidCreateContinuousTextConversation> Create(IUser user, IGuild guild, int frequency = 0)
        {
            string sendMessage = $"Raid Setup:\n" +
                "Please enter the whole raid in one message. Different fields are noticed by line breaks.\n" +
                "<Title>\n" +
                "<Channel>\n" +
                $"<Date> {Constants.DateFormat}\n" +
                $"<Duration in hours> dezimal numbers sperated with \",\"\n" +
                $"<Organisator>\n" +
                $"<Guild>\n" +
                $"<Voice Chat>\n" +
                $"<Roles> one role per line. format: [amount]:[Role name]:[Role description] \n" +
                $"<Description> can be multi line";
            await UserExtensions.SendMessageAsync(user, sendMessage);

            //Create Conversation
            return new RaidCreateContinuousTextConversation(user, guild, frequency);
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
                case State.creation:
                    if (await CreateRaid(message))
                    {
                        string previewMessage = $"Raid preview: react with {Constants.SignOnEmoji} to create the raid or with {Constants.SignOffEmoji} to cancel.";
                        IUserMessage raidMessage = await UserExtensions.SendMessageAsync(_user, previewMessage, embed: _raid.CreateRaidMessage());
                        await raidMessage.AddReactionAsync(Constants.SignOnEmoji);
                        await raidMessage.AddReactionAsync(Constants.SignOffEmoji);
                        _state = State.acception;
                    }
                    else
                    {
                        await UserExtensions.SendMessageAsync(_user, $"Creation of the raid failed.");
                        Program.Conversations.Remove(_user.Username);
                    }
                    break;
                case State.acception:
                    if (message.Equals(Constants.SignOnEmoji.Name))
                    {
                        ulong raidId = await HelperFunctions.PostRaidMessageAsync(_channel, _raid);
                        PlannedRaids.AddRaid(_raid, _guild.Id, _channel.Id, raidId);
                        await UserExtensions.SendMessageAsync(_user, "Created the raid successfully.");
                    }
                    else
                    {
                        await UserExtensions.SendMessageAsync(_user, $"Creation of the raid canceled.");
                    }
                    Program.Conversations.Remove(_user.Username);
                    break;
            }
        }

        private async Task<bool> CreateRaid(string message)
        {
            StringReader strReader = new StringReader(message);
            if (!await SetTitleAsync(strReader.ReadLine())) return false;
            if (!await SetChannelAsync(strReader.ReadLine())) return false;
            if (!await SetDateAsync(strReader.ReadLine())) return false;
            if (!await SetDurationAsync(strReader.ReadLine())) return false;
            if (!await SetOrganisatorAsync(strReader.ReadLine())) return false;
            if (!await SetGuildAsync(strReader.ReadLine())) return false;
            if (!await SetVoiceChatAsync(strReader.ReadLine())) return false;


            string line = strReader.ReadLine();
            while (line != null && Parsers.TryParseRole(line, out int noPositions, out string roleName, out string roleDecription))
            {
                _raid.AddRole(noPositions, roleName, roleDecription);
                line = strReader.ReadLine();
            }
            if (_raid.Roles.Count == 0)
            {
                await UserExtensions.SendMessageAsync(_user, $"No role was found.");
                return false;
            }

            string description = string.Empty;
            while (line != null)
            {
                if (!string.IsNullOrEmpty(description))
                {
                    description += "\n";
                }
                description += line;
                line = strReader.ReadLine();
            }
            _raid.Description = description;
            return true;
        }

        private async Task<bool> SetTitleAsync(string message)
        {
            if (message == null)
            {
                await UserExtensions.SendMessageAsync(_user, $"No title was found.");
                return false;
            }
            _raid.Title = message;
            return true;
        }

        private async Task<bool> SetChannelAsync(string message)
        {
            if (message == null)
            {
                await UserExtensions.SendMessageAsync(_user, $"No channel was found.");
                return false;
            }
            bool channelFound = false;
            foreach (var channel in await _guild.GetTextChannelsAsync())
            {
                if (channel.Name.Equals(message))
                {
                    channelFound = true;
                    _channel = channel;
                }
            }
            if (!channelFound)
            {
                await UserExtensions.SendMessageAsync(_user, $"No channel with the Name {message} found.");
                return false;
            }
            return true;
        }

        private async Task<bool> SetDateAsync(string message)
        {
            if (message == null)
            {
                await UserExtensions.SendMessageAsync(_user, $"No date was found.");
                return false;
            }
            if (Parsers.TryParseDateTime(message, out DateTime startTime))
            {
                _raid.StartTime = startTime;
                return true;
            }
            await UserExtensions.SendMessageAsync(_user, $"Invalid date format. needed Format:{Constants.DateFormat}");
            return false;
        }

        private async Task<bool> SetDurationAsync(string message)
        {
            if (message == null)
            {
                await UserExtensions.SendMessageAsync(_user, $"No duration was found.");
                return false;
            }
            if (Parsers.TryParseDouble(message, out double duration))
            {
                _raid.RaidDuration = duration;
                return true;
            }
            await UserExtensions.SendMessageAsync(_user, "Invalid duration format. Please enter a number");
            return false;
        }

        private async Task<bool> SetOrganisatorAsync(string message)
        {
            if (message == null)
            {
                await UserExtensions.SendMessageAsync(_user, $"No organisator was found.");
                return false;
            }
            _raid.Organisator = message;
            return true;
        }

        private async Task<bool> SetGuildAsync(string message)
        {
            if (message == null)
            {
                await UserExtensions.SendMessageAsync(_user, $"No guild was found.");
                return false;
            }
            _raid.Guild = message;
            return true;
        }

        private async Task<bool> SetVoiceChatAsync(string message)
        {
            if (message == null)
            {
                await UserExtensions.SendMessageAsync(_user, $"No voice chat was found.");
                return false;
            }
            _raid.VoiceChat = message;
            return true;
        }
    }
}
