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
        public Action<SocketReaction> OnReactionAdded = delegate { };
        public Action<SocketReaction> OnReactionRemoved = delegate { };
        public Action OnReactionsCleared = delegate { };
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

        public bool Add(IUserMessage userMessage, Action<SocketReaction> onReactionAdded, Action<SocketReaction> onReactionRemoved = null, Action onReactionCleared = null)
        {
            if (userMessage == null)
                return false;
            
            return _messageData.TryAdd(userMessage.Id, new ReactionData()
            {
                OnReactionAdded = onReactionAdded ?? delegate { },
                OnReactionRemoved = onReactionRemoved ?? delegate { },
                OnReactionsCleared = onReactionCleared ?? delegate { }
            });
        }
        public void Remove(IUserMessage userMessage)
        {
            if(userMessage != null)
                _messageData.Remove(userMessage.Id);
        }


        private Task DiscordClient_ReactionAdded(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            var _ = Task.Run(() =>
            {
                try
                {
                    if (_messageData.TryGetValue(message.Id, out ReactionData reactionData))
                    {
                        reactionData.OnReactionAdded.Invoke(reaction);
                    }
                }
                catch { }
            });
            return Task.CompletedTask;
        }

        private Task DiscordClient_ReactionRemoved(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            var _ = Task.Run(() =>
            {
                try
                {
                    if (_messageData.TryGetValue(message.Id, out ReactionData reactionData))
                    {
                        reactionData.OnReactionRemoved.Invoke(reaction);
                    }
                }
                catch { }
            });
            return Task.CompletedTask;
        }

        private Task DiscordClient_ReactionsCleared(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel)
        {
            var _ = Task.Run(() =>
            {
                try
                {
                    if (_messageData.TryGetValue(message.Id, out ReactionData reactionData))
                    {
                        reactionData.OnReactionsCleared.Invoke();
                    }
                }
                catch { }
            });
            return Task.CompletedTask;
        }
    }
}
