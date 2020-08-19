using Discord;
using Discord.WebSocket;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace Raidbot.Services
{
    public class RoleService
    {
        private static readonly string _jsonFile = Path.Combine(Constants.SAVEFOLDER, "roleMessages.json");
        private static readonly string xmlFile = Path.Combine("RoleMessages.xml");

        private readonly Dictionary<ulong, string> _roleMessages;

        public RoleService()
        {
            if (File.Exists(_jsonFile))
            {
                string json = File.ReadAllText(_jsonFile);
                _roleMessages = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<ulong, string>>(json);
            }
            else
            {
                _roleMessages = new Dictionary<ulong, string>();
            }
        }

        public async Task PostMessage(ITextChannel channel, string messageType)
        {
            string messageText = "Use the reactions to get or remove a role.\n" +
                "If you want to remove a role but there is no reaction, add it and remove it again.";
            List<IEmote> emoteList = new List<IEmote>();

            XmlDocument doc = new XmlDocument();
            doc.Load(xmlFile);
            XmlNodeList nodeList = doc.SelectNodes($"/Messages/{messageType}/*");
            foreach (XmlNode node in nodeList)
            {
                string roleName = node.SelectSingleNode("Name").InnerText;
                IEmote emote = new Emoji(node.SelectSingleNode("Icon").InnerText);
                messageText += $"\n{emote}: {roleName}";
                emoteList.Add(emote);
                if (channel.Guild.Roles.Where(r => r.Name == roleName).Count() == 0)
                {
                    await channel.Guild.CreateRoleAsync(roleName, isMentionable: true);
                }
            }

            var message = await channel.SendMessageAsync(messageText);
            await message.AddReactionsAsync(emoteList.ToArray());
            _roleMessages.Add(message.Id, messageType);
            SaveRoles();
        }

        public void DeleteMessage(ulong messageId)
        {
            _roleMessages.Remove(messageId);
            SaveRoles();
        }

        public bool IsRoleMessage(ulong messageId)
        {
            return _roleMessages.ContainsKey(messageId);
        }

        public async Task SetRole(IGuild guild, IGuildUser user, SocketReaction reaction)
        {
            if (!_roleMessages.ContainsKey(reaction.MessageId)) return;
            string roleName = GetDiscordRole(reaction.Emote, _roleMessages[reaction.MessageId]);
            IRole role = guild.Roles.Where(x => x.Name == roleName).FirstOrDefault();
            await user.AddRoleAsync(role);
        }

        public async Task UnsetRole(IGuild guild, IGuildUser user, SocketReaction reaction)
        {
            if (!_roleMessages.ContainsKey(reaction.MessageId)) return;
            string roleName = GetDiscordRole(reaction.Emote, _roleMessages[reaction.MessageId]);
            IRole role = guild.Roles.Where(x => x.Name == roleName).FirstOrDefault();
            await user.RemoveRoleAsync(role);
        }

        private string GetDiscordRole(IEmote emote, string messageType)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(xmlFile);
            XmlNodeList nodeList = doc.SelectNodes($"/Messages/{messageType}/*");
            foreach (XmlNode node in nodeList)
            {
                if (emote.Equals(new Emoji(node.SelectSingleNode("Icon").InnerText)))
                {
                    return node.SelectSingleNode("Name").InnerText;
                }

            }
            return string.Empty;
        }

        private void SaveRoles()
        {
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(_roleMessages);
            File.WriteAllText(_jsonFile, json);
        }
    }
}
