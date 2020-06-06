using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace Raidbot
{
    class HelperFunctions
    {
        DiscordSocketClient _client;
        static HelperFunctions _helperFunctions;

        private HelperFunctions() { }

        public static HelperFunctions Instance()
        {
            if (_helperFunctions == null)
            {
                _helperFunctions = new HelperFunctions();
            }
            return _helperFunctions;
        }
        public void Init(DiscordSocketClient client)
        {
            _client = client;
        }

        public static async Task SendMessageToEveryRaidMember(Raid raid, string[] text, string reason = "")
        {
            if (text.Length > 0)
            {
                string message = $"{raid.Title} {reason}: ";
                foreach (string log in text)
                {
                    message += $" {log}";
                }
                await Instance().SendMessageToEveryRaidMember(raid, message);
            }
        }

        public async Task SendMessageToEveryRaidMember(Raid raid, string message)
        {
            foreach (var user in raid.Users)
            {
                if (user.Value.DiscordId != 0)
                {
                    await SendMessageToUser(message, user.Value.DiscordId);
                }
            }
        }

        public async Task SendMessageToUser(string message, ulong userId)
        {
            if (_client != null)
            {
                await _client.GetUser(userId).SendMessageAsync(message);
            }
        }

        public SocketChannel GetChannelById(ulong channelId)
        {
            if (_client != null)
            {
                return _client.GetChannel(channelId);
            }
            return null;
        }

        public SocketGuild GetGuildById(ulong guildId)
        {
            if (_client != null)
            {
                return _client.GetGuild(guildId);
            }
            return null;
        }

        public async Task<IGuildUser> GetGuildUserByIds(ulong guildId, ulong userId)
        {
            if (_client != null)
            {
                IGuild guild = GetGuildById(guildId);
                return await guild.GetUserAsync(userId);
            }
            return null;
        }

        public async Task<ulong> RepostRaidMessage(Raid raid)
        {
            SocketTextChannel channel = (SocketTextChannel)GetChannelById(raid.ChannelId);
            IUserMessage userMessage = (IUserMessage)await channel.GetMessageAsync(raid.MessageId);
            await userMessage.DeleteAsync();
            return await PostRaidMessageAsync(channel, raid);
        }

        public static async Task<ulong> PostRaidMessageAsync(ITextChannel channel, Raid raid)
        {
            var raidMessage = await channel.SendMessageAsync(embed: raid.CreateRaidMessage());
            await raidMessage.AddReactionAsync(Constants.SignOnEmoji);
            await raidMessage.AddReactionAsync(Constants.UnsureEmoji);
            await raidMessage.AddReactionAsync(Constants.BackupEmoji);
            await raidMessage.AddReactionAsync(Constants.FlexEmoji);
            await raidMessage.AddReactionAsync(Constants.SignOffEmoji);
            return raidMessage.Id;
        }
    }
}
