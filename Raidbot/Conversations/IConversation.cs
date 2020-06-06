using System;
using System.Collections.Generic;
using System.Text;

namespace Raidbot
{
    interface IConversation
    {
        public void ProcessMessage(string message);
    }
}
