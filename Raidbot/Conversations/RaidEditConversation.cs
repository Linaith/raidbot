using Discord;
using Raidbot.Models;
using Raidbot.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Raidbot.Conversations
{
    public class RaidEditConversation : ConversationBase
    {
        public enum Edits { Title, Description, Time, Duation, Organisator, Guild, VoiceChat }

        private readonly Raid _raid;
        private readonly Edits _edit;
        private readonly IUserMessage _userMessage;
        private readonly RaidService _raidService;

        private RaidEditConversation(ConversationService conversationService, RaidService raidService, IUser user, string raidId, Edits edit, IUserMessage userMessage) : base(conversationService, user)
        {
            this._edit = edit;
            this._userMessage = userMessage;
            _raidService = raidService;
            if (!_raidService.TryFindRaid(raidId, out _raid)) throw new KeyNotFoundException("The raid for this message was not found!"); ;
        }

        public static async Task<RaidEditConversation> Create(ConversationService conversationService, RaidService raidService, IUser user, string raidId, Edits edit, IUserMessage userMessage)
        {
            //Create Conversation
            RaidEditConversation conversation = new RaidEditConversation(conversationService, raidService, user, raidId, edit, userMessage);

            //send edit Message
            string message = $"You are editing the {edit} of Raid {conversation._raid.RaidId}.\n" +
                $"you can write \"cancel\" to cancel the editing\n" +
                $"the current {edit} is: \n";
            switch (edit)
            {
                case Edits.Description:
                    message += conversation._raid.Description;
                    break;
                case Edits.Duation:
                    message += conversation._raid.RaidDuration;
                    break;
                case Edits.Time:
                    message += conversation._raid.StartTime.ToString() +
                        $"\nrequired Format: {Constants.DateFormat}";
                    break;
                case Edits.Title:
                    message += conversation._raid.Title;
                    break;
                case Edits.Organisator:
                    message += conversation._raid.Organisator;
                    break;
                case Edits.Guild:
                    message += conversation._raid.Guild;
                    break;
                case Edits.VoiceChat:
                    message += conversation._raid.VoiceChat;
                    break;
            }
            await UserExtensions.SendMessageAsync(conversation._user, message);

            return conversation;
        }

        protected override async Task ProcessUncanceledMessage(string message)
        {
            switch (_edit)
            {
                case Edits.Description:
                    _raid.Description = message;
                    break;
                case Edits.Duation:
                    if (!await EditDuration(message)) return;
                    break;
                case Edits.Time:
                    if (!await EditTime(message)) return;
                    break;
                case Edits.Title:
                    _raid.Title = message;
                    break;
                case Edits.Organisator:
                    _raid.Organisator = message;
                    break;
                case Edits.Guild:
                    _raid.Guild = message;
                    break;
                case Edits.VoiceChat:
                    _raid.VoiceChat = message;
                    break;
            }
            try
            {
                await _userMessage.ModifyAsync(msg => msg.Embed = _raid.CreateRaidMessage());
                await UserExtensions.SendMessageAsync(_user, "Successfully modified the raid.");
            }
            catch { }
            finally
            {
                _conversationService.CloseConversation(_user.Id);
                _raidService.UpdateRaid(_raid.RaidId, _raid);
            }
        }

        private async Task<bool> EditDuration(string message)
        {
            if (Parsers.TryParseDouble(message, out double duration))
            {
                _raid.RaidDuration = duration;
                return true;
            }
            else
            {
                await UserExtensions.SendMessageAsync(_user, "Invalid Format. Please enter a Number");
                return false;
            }
        }

        private async Task<bool> EditTime(string message)
        {
            if (Parsers.TryParseDateTime(message, out DateTime time))
            {
                _raid.StartTime = time;
                foreach (RaidReminder reminder in _raid.Reminders.Values)
                {
                    reminder.Sent = false;
                }
                return true;
            }
            else
            {
                await UserExtensions.SendMessageAsync(_user, $"Invalid Format. Format: {Constants.DateFormat}");
                return false;
            }
        }
    }
}
