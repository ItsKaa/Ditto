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
        private ObjectLock<DiscordClientEx> _discordClient = null;
        private Dictionary<ulong, ReactionData> _messageData = new Dictionary<ulong, ReactionData>();

        public async Task SetupAsync(ObjectLock<DiscordClientEx> discordClient)
        {
            _discordClient = discordClient;
            await _discordClient.DoAsync((client) =>
            {
                client.ReactionAdded += DiscordClient_ReactionAdded;
                client.ReactionRemoved += DiscordClient_ReactionRemoved;
                client.ReactionsCleared += DiscordClient_ReactionsCleared;
            }).ConfigureAwait(false);
        }
        public void Uninstall()
        {
            _discordClient?.Do((client) =>
            {
                if (client != null)
                {
                    client.ReactionAdded -= DiscordClient_ReactionAdded;
                    client.ReactionRemoved -= DiscordClient_ReactionRemoved;
                    client.ReactionsCleared -= DiscordClient_ReactionsCleared;
                }
            });
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
