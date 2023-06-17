using Discord;
using Discord.WebSocket;
using Ditto.Data;
using Ditto.Data.Discord;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ditto.Bot.Services
{
    public class ReactionData
    {
        public Func<SocketReaction, Task> OnReactionAdded = delegate { return Task.CompletedTask; };
        public Func<SocketReaction, Task> OnReactionRemoved = delegate { return Task.CompletedTask; };
        public Func<Task> OnReactionsCleared = delegate { return Task.CompletedTask; };
    }
    public class ReactionHandler : IDisposable
    {
        private DiscordClientEx _discordClient = null;
        private Dictionary<ulong, ReactionData> _messageData = new Dictionary<ulong, ReactionData>();

        public async Task SetupAsync(DiscordClientEx discordClient)
        {
            _discordClient = discordClient;
            _discordClient.ReactionAdded += DiscordClient_ReactionAdded;
            _discordClient.ReactionRemoved += DiscordClient_ReactionRemoved;
            _discordClient.ReactionsCleared += DiscordClient_ReactionsCleared;
        }
        public void Uninstall()
        {
            if (_discordClient != null)
            {
                _discordClient.ReactionAdded -= DiscordClient_ReactionAdded;
                _discordClient.ReactionRemoved -= DiscordClient_ReactionRemoved;
                _discordClient.ReactionsCleared -= DiscordClient_ReactionsCleared;
            }
            _messageData.Clear();
        }
        public void Dispose()
        {
            try { Uninstall(); } catch { }
        }

        public bool Add(IUserMessage userMessage, Func<SocketReaction, Task> onReactionAdded, Func<SocketReaction, Task> onReactionRemoved = null, Func<Task> onReactionCleared = null)
        {
            if (userMessage == null)
                return false;
            
            return _messageData.TryAdd(userMessage.Id, new ReactionData()
            {
                OnReactionAdded = onReactionAdded ?? delegate { return Task.CompletedTask; },
                OnReactionRemoved = onReactionRemoved ?? delegate { return Task.CompletedTask; },
                OnReactionsCleared = onReactionCleared ?? delegate { return Task.CompletedTask; },
            });
        }
        public void Remove(IUserMessage userMessage)
        {
            if(userMessage != null)
                _messageData.Remove(userMessage.Id);
        }


        private Task DiscordClient_ReactionAdded(Cacheable<IUserMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction)
        {
            var _ = Task.Run(async () =>
            {
                try
                {
                    if (_messageData.TryGetValue(message.Id, out ReactionData reactionData))
                    {
                        await reactionData.OnReactionAdded(reaction).ConfigureAwait(false);
                    }
                }
                catch { }
            });
            return Task.CompletedTask;
        }

        private Task DiscordClient_ReactionRemoved(Cacheable<IUserMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction)
        {
            var _ = Task.Run(async () =>
            {
                try
                {
                    if (_messageData.TryGetValue(message.Id, out ReactionData reactionData))
                    {
                        await reactionData.OnReactionRemoved(reaction).ConfigureAwait(false);
                    }
                }
                catch { }
            });
            return Task.CompletedTask;
        }

        private Task DiscordClient_ReactionsCleared(Cacheable<IUserMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel)
        {
            var _ = Task.Run(async() =>
            {
                try
                {
                    if (_messageData.TryGetValue(message.Id, out ReactionData reactionData))
                    {
                        await reactionData.OnReactionsCleared().ConfigureAwait(false);
                    }
                }
                catch { }
            });
            return Task.CompletedTask;
        }
    }
}
