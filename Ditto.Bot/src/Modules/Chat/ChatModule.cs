using Discord.Commands;
using Ditto.Attributes;
using Ditto.Data.Chatting;
using Ditto.Data.Commands;
using Ditto.Data.Discord;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace Ditto.Bot.Modules.Chat
{
    [Alias("chat")]
    class ChatModule : DiscordModule
    {
        public static ConcurrentDictionary<ulong, Lazy<CleverbotSession>> CleverbotSessions { get; private set; }

        static ChatModule()
        {
            Ditto.Connected += async () =>
            {
                CleverbotSessions = new ConcurrentDictionary<ulong, Lazy<CleverbotSession>>(
                    await Ditto.Client.DoAsync((c) => c.Guilds.ToDictionary(g => g.Id,
                        gc => new Lazy<CleverbotSession>(
                            () => new CleverbotSession(Ditto.Settings.Credentials.CleverbotApiKey, false), true
                        )
                    )).ConfigureAwait(false)
                );
            };

            Ditto.Client.Do((c) => c.JoinedGuild += (guild) =>
            {
                if (guild != null)
                {
                    CleverbotSessions.TryAdd(guild.Id, new Lazy<CleverbotSession>(
                        () => new CleverbotSession(Ditto.Settings.Credentials.CleverbotApiKey, false
                    )));
                }
                return Task.CompletedTask;
            });

            Ditto.Exit += () =>
            {
                CleverbotSessions.Clear();
                return Task.CompletedTask;
            };
        }

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.All, RequireBotTag = true)]
        public async Task Talk([Multiword] string message)
        {
            try
            {
                if (CleverbotSessions.TryGetValue(Context.Guild.Id, out Lazy<CleverbotSession> cleverbot))
                {
                    if (cleverbot.Value.Valid)
                    {
                        await Context.Channel.TriggerTypingAsync().ConfigureAwait(false);
                        var response = await cleverbot.Value.GetResponseAsync(message).ConfigureAwait(false);
                        await Context.Channel.SendMessageAsync(response.Response).ConfigureAwait(false);
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
        }
    }
}
