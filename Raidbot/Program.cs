using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Raidbot.Services;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Raidbot
{
    class Program
    {
        public static void Main()
        => new Program().MainAsync().GetAwaiter().GetResult();

        private DiscordSocketClient _client;

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
                .AddSingleton<UserService>()
                .AddSingleton<LogService>()
                .AddSingleton<ConversationService>()
                .AddSingleton<TimerService>()
                .AddSingleton<RaidService>()
                .BuildServiceProvider();

#if DEBUG
            var token = File.ReadAllText("debugtoken.txt");
#else
            var token = File.ReadAllText("token.txt");
#endif

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            serviceProvider.GetRequiredService<TimerService>().Start();
            await serviceProvider.GetRequiredService<CommandHandler>().InstallCommandsAsync();

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
