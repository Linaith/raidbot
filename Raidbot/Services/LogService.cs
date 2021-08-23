using Discord.WebSocket;
using Raidbot.Models;
using Raidbot.Users;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Raidbot.Services
{
    public class LogService
    {
        //Montags static RaidId: 1840383525
        private readonly DiscordSocketClient _client;
        private readonly UserService _userService;

        public LogService(UserService userService, DiscordSocketClient client)
        {
            _client = client;
            _userService = userService;
        }

        public async Task LogRaid(string message, Raid raid)
        {
            await WriteToChat(message, raid);
            if (raid != null)
            {
                WriteToRaidFile(message, raid);
            }
        }

        private void WriteToRaidFile(string message, Raid raid)
        {

        }

        private async Task WriteToChat(string message, Raid raid)
        {
            DiscordServer server = _userService.GetServer(raid.GuildId);
            if (server.LogChannelId != 0)
            {
                SocketTextChannel channel = (SocketTextChannel)_client.GetChannel(server.LogChannelId);
                if (channel != null)
                {
                    await channel.SendMessageAsync($"{raid.Title}: {message}");
                }
            }
        }
    }
}
