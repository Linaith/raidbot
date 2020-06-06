using Discord;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Raidbot
{
    public static class DiscordRoles
    {
        private static readonly string _jsonFile = Path.Combine(Constants.SAVEFOLDER, "roleMessages.json");

        //  guildId, RoleMessage
        private static Dictionary<ulong, RoleMessage> _roleMessages = new Dictionary<ulong, RoleMessage>();

        private class RoleMessage
        {
            public ulong ChannelId { get; set; }
            public ulong MessageId { get; set; }

            public RoleMessage(ulong channelId, ulong messageId)
            {
                ChannelId = channelId;
                MessageId = messageId;
            }
        }

        private static readonly Dictionary<IEmote, string> _roles = new Dictionary<IEmote, string>()
        {
            {new Emoji("\u2611"), "Raid"},
            {new Emoji("\u0031\uFE0F\u20E3"), "w1"},
            {new Emoji("\u0032\uFE0F\u20E3"), "w2"},
            {new Emoji("\u0033\uFE0F\u20E3"), "w3"},
            {new Emoji("\u0034\uFE0F\u20E3"), "w4"},
            {new Emoji("\u0035\uFE0F\u20E3"), "w5"},
            {new Emoji("\u0036\uFE0F\u20E3"), "w6"},
            {new Emoji("\u0037\uFE0F\u20E3"), "w7"},
        //    {new Emoji("\u0038\uFE0F\u20E3"), "w8"},
        //    {new Emoji("\u0039\uFE0F\u20E3"), "w9"},
            {new Emoji("♾️"), "w1-7"},
            {new Emoji("🇹"), "Raid Training"},
            {new Emoji("\u203C"), "Fractal Training"},
            {new Emoji("\uD83D\uDCAF"), "Fractals"},
            {new Emoji("🏴‍☠️"), "Dungeons"},
            {new Emoji("⚔️"), "StrikeMissions"}
        };

        static DiscordRoles()
        {
            LoadRoles();
        }

        public static bool IsRoleMessage(ulong guildId, ulong messageId)
        {
            if (_roleMessages.ContainsKey(guildId))
            {
                return _roleMessages[guildId].MessageId == messageId;
            }
            return false;
        }

        public static async Task PostRoleMessage(ITextChannel channel)
        {
            if (_roleMessages.ContainsKey(channel.GuildId))
            {
                await DeleteMessage(channel.Guild);
            }
            await InitServer(channel.Guild);
            await PostMessage(channel);
        }

        public static async Task DeleteRoleMessage(IGuild guild)
        {
            await DeleteMessage(guild);
            await ResetAllRoles(guild);
        }

        private static async Task DeleteMessage(IGuild guild)
        {
            var channel = await guild.GetTextChannelAsync(_roleMessages[guild.Id].ChannelId);
            await channel.DeleteMessageAsync(_roleMessages[guild.Id].MessageId);
            _roleMessages.Remove(guild.Id);
            SaveRoles();
        }

        private static async Task PostMessage(ITextChannel channel)
        {
            string messageText = "Use the reactions to get or remove a role.\n" +
                "If you want to remove a role but there is no reaction, add it and remove it again.";
            foreach (var role in _roles)
            {
                messageText += $"\n{role.Key}: {role.Value}";
            }
            var message = await channel.SendMessageAsync(messageText);
            await message.AddReactionsAsync(_roles.Keys.ToArray());
            _roleMessages.Add(channel.GuildId, new RoleMessage(channel.Id, message.Id));
            SaveRoles();
        }

        public static async Task InitServer(IGuild guild)
        {
            foreach (string roleName in _roles.Values)
            {
                if (guild.Roles.Where(r => r.Name == roleName).Count() == 0)
                {
                    await guild.CreateRoleAsync(roleName, isMentionable: true);
                }
            }
        }

        public static async Task SetRole(IGuild guild, IGuildUser user, IEmote emote)
        {
            if (!_roles.ContainsKey(emote)) return;
            string roleName = _roles[emote];
            foreach (IRole role in guild.Roles)
            {
                if (roleName == role.Name)
                {
                    await user.AddRoleAsync(role);
                    return;
                }
            }
            await SetRole(guild, user, emote);
        }

        public static async Task UnsetRole(IGuild guild, IGuildUser user, IEmote emote)
        {
            if (!_roles.ContainsKey(emote)) return;
            string roleName = _roles[emote];
            foreach (IRole role in guild.Roles)
            {
                if (roleName == role.Name)
                {
                    await user.RemoveRoleAsync(role);
                    return;
                }
            }
            await UnsetRole(guild, user, emote);
        }

        private static async Task ResetAllRoles(IGuild guild)
        {
            List<IRole> rolesToRemove = new List<IRole>();
            foreach (string roleName in _roles.Values)
            {
                rolesToRemove.Add(guild.Roles.Where(r => r.Name == roleName).Single());
            }

            foreach (IGuildUser user in await guild.GetUsersAsync())
            {
                await user.RemoveRolesAsync(rolesToRemove);
            }
        }

        private static void SaveRoles()
        {
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(_roleMessages);
            File.WriteAllText(_jsonFile, json);
        }

        private static void LoadRoles()
        {
            if (File.Exists(_jsonFile))
            {
                string json = File.ReadAllText(_jsonFile);
                _roleMessages = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<ulong, RoleMessage>>(json);
            }
        }
    }
}
