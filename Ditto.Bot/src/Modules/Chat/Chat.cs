using Ditto.Data.Chatting;
using Ditto.Extensions;
using System.Collections.Concurrent;
using System;
using Ditto.Data.Discord;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Ditto.Bot.Data.API.Rest;

namespace Ditto.Bot.Modules.Chat
{
    public class Chat : DiscordModule
    {
        public const int PruneConfirmationMessageCount = 10;
        public static ConcurrentDictionary<ulong, Lazy<CleverbotSession>> CleverbotSessions { get; private set; }

        static Chat()
        {
            Ditto.Connected += () =>
            {
                CleverbotSessions = new ConcurrentDictionary<ulong, Lazy<CleverbotSession>>(
                    Ditto.Client.Guilds.ToDictionary(g => g.Id, _ => new Lazy<CleverbotSession>(
                        () => new CleverbotSession(Ditto.Settings.Credentials.CleverbotApiKey, false), true)
                    ));
                return Task.CompletedTask;
            };

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

            Ditto.Exit += () =>
            {
                CleverbotSessions?.Clear();
                return Task.CompletedTask;
            };
        }

        public static string GetPruneConfirmationMessage(IUser user, int count)
            => $"{user.Mention} Please verify. Do you wish to delete {count} messages from this channel?";

        public static async Task<string> Talk(IGuild guild, IMessageChannel channel, string message)
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

        public static string Insult(string name)
        {
            var insult = new InsultApi().Insult("");
            return !string.IsNullOrEmpty(insult?.Insult)
                ? name + insult.Insult
                : null;
        }


        public static async Task<int> PruneMessagesAsync(ITextChannel channel, int count, IUser user = null, string pattern = "")
        {
            if (count > 100)
                count = 100;
            else if (count <= 0)
                count = 1;

            var deletedCount = 0;

            // First attempt to delete it by bulk (only 14 days)
            try
            {
                var list14Days = (await channel.GetMessagesAsync(count).ToListAsync())
                    .SelectMany(i => i)
                    .Where(x => !x.IsPinned
                        && Math.Abs((DateTime.Now - x.CreatedAt.UtcDateTime).TotalDays) < 14
                        && x.Flags?.HasFlag(MessageFlags.Ephemeral) == false
                    );

                if (!string.IsNullOrEmpty(pattern))
                {
                    list14Days = list14Days.Where(i => i.Content.Contains(pattern));
                }

                if (user != null)
                {
                    list14Days = list14Days.Where(i => i.Author?.Id == user.Id);
                }

                // Attempt to bulk delete
                await channel.DeleteMessagesAsync(list14Days).ConfigureAwait(false);
                deletedCount += list14Days.Count();

                if (count <= deletedCount)
                    return deletedCount;

                // Wait a little bit because it takes some time and it's not in the async await.
                await Task.Delay(2000).ConfigureAwait(false);
            }
            catch { }

            // Manually delete older messages
            var list = (await channel.GetMessagesAsync(count - deletedCount).ToListAsync())
                .SelectMany(i => i)
            .Where(x => !x.IsPinned);

            if (!string.IsNullOrEmpty(pattern))
            {
                list = list.Where(i => i.Content.Contains(pattern));
            }

            if (user != null)
            {
                list = list.Where(i => i.Author?.Id == user.Id);
            }

            foreach (var msg in list)
            {
                try
                {
                    if (msg.Flags?.HasFlag(MessageFlags.Ephemeral) == false)
                    {
                        await msg.DeleteAsync().ConfigureAwait(false);
                        deletedCount++;
                    }
                }
                catch { }
            }

            return deletedCount;
        }
    }
}
