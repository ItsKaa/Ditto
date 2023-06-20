using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace Ditto.Bot.Services.Data
{
    public class ReactionData
    {
        public Func<SocketReaction, Task> OnReactionAdded = delegate { return Task.CompletedTask; };
        public Func<SocketReaction, Task> OnReactionRemoved = delegate { return Task.CompletedTask; };
        public Func<Task> OnReactionsCleared = delegate { return Task.CompletedTask; };
    }
}
