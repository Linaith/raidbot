using System.Threading.Tasks;

namespace Raidbot.Conversations
{
    interface IConversation
    {
        public Task ProcessMessage(string message);
    }
}
