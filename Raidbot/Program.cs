using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Raidbot.Services;
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
            if (!Directory.Exists(Constants.SAVEFOLDER))
            {
                Directory.CreateDirectory(Constants.SAVEFOLDER);
            }
            _client = new DiscordSocketClient();
            _client.Log += Log;


            IServiceProvider serviceProvider = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandler>()
                .AddSingleton<RoleService>()
                .BuildServiceProvider();

#if DEBUG
            var token = File.ReadAllText("debugtoken.txt");
#else
            var token = File.ReadAllText("token.txt");
#endif

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            await serviceProvider.GetRequiredService<CommandHandler>().InstallCommandsAsync();

            HelperFunctions.Instance().Init(_client);
            Reminder.Start();

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
