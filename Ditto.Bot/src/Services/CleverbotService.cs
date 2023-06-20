using Ditto.Data.Chatting;
using Ditto.Extensions;
using System.Collections.Concurrent;
using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;

namespace Ditto.Bot.Services
{
    public class CleverbotService : IDittoService
    {
        public const int PruneConfirmationMessageCount = 10;
        public static ConcurrentDictionary<ulong, Lazy<CleverbotSession>> CleverbotSessions { get; private set; }

        public Task Initialised() => Task.CompletedTask;

        public Task Connected()
        {
            CleverbotSessions = new ConcurrentDictionary<ulong, Lazy<CleverbotSession>>(
                Ditto.Client.Guilds.ToDictionary(g => g.Id, _ => new Lazy<CleverbotSession>(
                    () => new CleverbotSession(Ditto.Settings.Credentials.CleverbotApiKey, false), true)
                ));

            Ditto.Client.JoinedGuild += (guild) =>
            {
                if (guild != null)
                {
                    CleverbotSessions.TryAdd(guild.Id, new Lazy<CleverbotSession>(
                        () => new CleverbotSession(Ditto.Settings.Credentials.CleverbotApiKey, false), true)
                    );
                }
                return Task.CompletedTask;
            };

            Ditto.Client.LeftGuild += (guild) =>
            {
                if (guild != null)
                {
                    CleverbotSessions.TryRemove(guild.Id, out Lazy<CleverbotSession> session);
                }
                return Task.CompletedTask;
            };

            return Task.CompletedTask;
        }

        public Task Exit()
        {
            CleverbotSessions?.Clear();
            return Task.CompletedTask;
        }

        public string GetPruneConfirmationMessage(IUser user, int count)
            => $"{user.Mention} Please verify. Do you wish to delete {count} messages from this channel?";

        public async Task<string> Talk(IGuild guild, IMessageChannel channel, string message)
        {
            try
            {
                if (CleverbotSessions.TryGetValue(guild.Id, out Lazy<CleverbotSession> cleverbot))
                {
                    if (cleverbot.Value.Valid)
                    {
                        await channel.TriggerTypingAsync().ConfigureAwait(false);
                        var tags = message.ParseDiscordTags();
                        var response = await cleverbot.Value.GetResponseAsync(message).ConfigureAwait(false);
                        return response.Response;
                    }
                    else
                    {
                        throw new Exception("Invalid Cleverbot API key.");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }

            return null;
        }
    }
}
