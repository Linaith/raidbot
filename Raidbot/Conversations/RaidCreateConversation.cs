using Discord;
using Raidbot.Services;
using Raidbot.Users;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Raidbot.Conversations
{
    class RaidCreateConversation : ConversationBase
    {
        enum State { title, description, channel, date, duration, organisator, guild, voicechat, accountType, roles }

        private ITextChannel _channel;
        private readonly Raid _raid;
        private readonly IGuild _guild;
        private State _state;
        private readonly RaidService _raidService;
        private readonly UserService _userService;

        private RaidCreateConversation(ConversationService conversationService, RaidService raidService, UserService userService, IUser user, IGuild guild, int frequency) : base(conversationService, user)
        {
            _raid = new Raid(_raidService.CreateRaidId(), frequency);
            _state = State.title;
            _guild = guild;
            _raidService = raidService;
            _userService = userService;
        }

        public static async Task<RaidCreateConversation> Create(ConversationService conversationService, RaidService raidService, UserService userService, IUser user, IGuild guild, int frequency)
        {
            string sendMessage = "Raid Setup:\n" +
"You can type \"cancel\" at any point during this process to cancel the raid setup\n\n" +
"Enter the name for raid run:";
            await UserExtensions.SendMessageAsync(user, sendMessage);

            //Create Conversation
            return new RaidCreateConversation(conversationService, raidService, userService, user, guild, frequency);
        }

        protected override async Task ProcessUncanceledMessage(string message)
        {
            switch (_state)
            {
                case State.title:
                    await ProcessTitleAsync(message);
                    break;
                case State.description:
                    await ProcessDescriptionAsync(message);
                    break;
                case State.channel:
                    await ProcessChannelAsync(message);
                    break;
                case State.date:
                    await ProcessDateAsync(message);
                    break;
                case State.duration:
                    await ProcessDurationAsync(message);
                    break;
                case State.organisator:
                    await ProcessOrganisatorAsync(message);
                    break;
                case State.guild:
                    await ProcessGuildAsync(message);
                    break;
                case State.voicechat:
                    await ProcessVoiceChatAsync(message);
                    break;
                case State.accountType:
                    await ProcessAccountTypeAsync(message);
                    break;
                case State.roles:
                    await ProcessRolesAsync(message);
                    break;
            }
        }

        public async Task ProcessTitleAsync(string message)
        {
            _raid.Title = message;
            await UserExtensions.SendMessageAsync(_user, "Enter description for raid run:");
            _state = State.description;
        }

        public async Task ProcessDescriptionAsync(string message)
        {
            _raid.Description = message;
            await UserExtensions.SendMessageAsync(_user, "Enter the channel for raid run announcement:");
            _state = State.channel;
        }

        public async Task ProcessChannelAsync(string message)
        {
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
                return;
            }
            await UserExtensions.SendMessageAsync(_user, $"Enter the date and time for raid run ({Constants.DateFormat}):");
            _state = State.date;
        }

        public async Task ProcessDateAsync(string message)
        {
            if (Parsers.TryParseDateTime(message, out DateTime startTime))
            {
                _raid.StartTime = startTime;
                await UserExtensions.SendMessageAsync(_user, "Enter the duration of the raid in hours:\ndezimal numbers sperated with \",\"");
                _state = State.duration;
            }
            else
            {
                await UserExtensions.SendMessageAsync(_user, $"Invalid Format. needed Format:{Constants.DateFormat}");
            }
        }

        public async Task ProcessDurationAsync(string message)
        {
            if (Parsers.TryParseDouble(message, out double duration))
            {
                _raid.RaidDuration = duration;
                await UserExtensions.SendMessageAsync(_user, "Who is the raid leader:");
                _state = State.organisator;
            }
            else
            {
                await UserExtensions.SendMessageAsync(_user, "Invalid Format. Please enter a Number");
            }
        }

        public async Task ProcessOrganisatorAsync(string message)
        {
            _raid.Organisator = message;
            await UserExtensions.SendMessageAsync(_user, "Which guild is organising the raid:");
            _state = State.guild;
        }

        public async Task ProcessGuildAsync(string message)
        {
            _raid.Guild = message;
            await UserExtensions.SendMessageAsync(_user, "Enter the used voice chat:");
            _state = State.voicechat;
        }

        public async Task ProcessVoiceChatAsync(string message)
        {
            _raid.VoiceChat = message;
            if (_userService.ListAccountTypes(_guild.Id).Count == 1)
            {
                await UserExtensions.SendMessageAsync(_user, "Enter the roles for raid run (format: [amount]:[Role name]:[Role description]). Type done to finish entering roles:");
                _state = State.roles;
                _raid.AccountType = _userService.ListAccountTypes(_guild.Id).First();
            }
            else
            {
                await UserExtensions.SendMessageAsync(_user, $"Choose the account type used for the raid. Existing account types: {CreateAccountTypeString()}");
                _state = State.accountType;
            }
        }

        public async Task ProcessAccountTypeAsync(string message)
        {
            if (_userService.ListAccountTypes(_guild.Id).Contains(message))
            {
                _raid.AccountType = message;
                await UserExtensions.SendMessageAsync(_user, "Enter the roles for raid run (format: [amount]:[Role name]). Type done to finish entering roles:");
                _state = State.roles;
            }
            else
            {
                await UserExtensions.SendMessageAsync(_user, "Invalid account type. \n" +
                    "Please try again or type \"cancel\" to cancel the interaction.\n" +
                    $"Available account types: {CreateAccountTypeString()}");
            }
        }

        public async Task ProcessRolesAsync(string message)
        {
            if (message.Equals("done", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    ulong raidId = await _raidService.PostRaidMessageAsync(_channel, _raid);
                    _raidService.AddRaid(_raid, _guild.Id, _channel.Id, raidId);
                    await UserExtensions.SendMessageAsync(_user, "Created the raid successfully.");
                }
                catch { }
                finally
                {
                    _conversationService.CloseConversation(_user.Id);
                }
                return;
            }
            string invalidFormatMessage = "Invalid format. (format: [amount]:[Role name]). Type \"done\" to finish entering roles:";
            try
            {
                if (!Parsers.TryParseRole(message, out int noPositions, out string roleName, out string roleDescription))
                {
                    await UserExtensions.SendMessageAsync(_user, invalidFormatMessage);
                    return;
                }
                if (_raid.AddRole(noPositions, roleName, roleDescription))
                {
                    await UserExtensions.SendMessageAsync(_user, "Role added, please enter the next role or type \"done\"");
                }
                else
                {
                    await UserExtensions.SendMessageAsync(_user, "Adding of the role failed! Maybe it did already exist.\nPlease enter the next role or type \"done\"");
                }
            }
            catch
            {
                await UserExtensions.SendMessageAsync(_user, invalidFormatMessage);
            }
        }

        private string CreateAccountTypeString()
        {
            string accountTypes = string.Empty;
            foreach (string accountType in _userService.ListAccountTypes(_guild.Id))
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
