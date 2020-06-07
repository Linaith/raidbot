using Discord;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

namespace Raidbot
{
    public class RaidEditConversation : IConversation
    {
        public enum Edits { Title, Description, Time, Duation, Organisator, Guild, VoiceChat }

        private readonly IUser _user;
        private readonly Raid _raid;
        private readonly Edits _edit;
        private readonly IUserMessage _userMessage;

        private RaidEditConversation(IUser user, string raidId, Edits edit, IUserMessage userMessage)
        {
            this._user = user;
            this._edit = edit;
            this._userMessage = userMessage;
            if (!PlannedRaids.TryFindRaid(raidId, out _raid)) throw new KeyNotFoundException("The raid for this message was not found!"); ;
        }

        public static async Task<RaidEditConversation> Create(IUser user, string raidId, Edits edit, IUserMessage userMessage)
        {
            //Create Conversation
            RaidEditConversation conversation = new RaidEditConversation(user, raidId, edit, userMessage);

            //send edit Message
            string message = $"You are editing the {edit} of Raid {conversation._raid.RaidId}.\n" +
                $"write can write cancel to cancel the editing\n" +
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
                    message += conversation._raid.StartTime.ToString();
                    break;
                case Edits.Title:
                    message += conversation._raid.Title + "\n";
                    message += $"required Format: {Constants.DateFormat}";
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

        public async void ProcessMessage(string message)
        {
            if (message.Equals("cancel", StringComparison.OrdinalIgnoreCase))
            {
                await UserExtensions.SendMessageAsync(_user, "interaction canceled");
                Program.Conversations.Remove(_user.Username);
                return;
            }

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
                Program.Conversations.Remove(_user.Username);
                PlannedRaids.UpdateRaid(_raid.RaidId, _raid);
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
                _raid.ReminderSent = false;
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
