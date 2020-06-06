using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Raidbot
{
    class Program
    {

        public static void Main()
        => new Program().MainAsync().GetAwaiter().GetResult();

        private DiscordSocketClient _client;
        //public static List<Raid> PlannedRaids = new List<Raid>();
        //user, conversation
        public static Dictionary<string, IConversation> Conversations = new Dictionary<string, IConversation>();

        public async Task MainAsync()
        {
            _client = new DiscordSocketClient();

            _client.Log += Log;

#if DEBUG
            var token = File.ReadAllText("debugtoken.txt");
#else
            var token = File.ReadAllText("token.txt");
#endif

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();
            Reminder.Start();

            if (!Directory.Exists(Constants.SAVEFOLDER))
            {
                Directory.CreateDirectory(Constants.SAVEFOLDER);
            }

            CommandService commandService = new CommandService();
            CommandHandler commandHandler = new CommandHandler(_client, commandService);
            await commandHandler.InstallCommandsAsync();
            HelperFunctions.Instance().Init(_client);

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
