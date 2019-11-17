using Discord;
using Ditto.Attributes;
using Ditto.Bot.Data.API.Rest;
using Ditto.Data.Chatting;
using Ditto.Data.Commands;
using Ditto.Data.Discord;
using Ditto.Extensions;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace Ditto.Bot.Modules.Chat
{
    public class Chat : DiscordModule
    {
        public static ConcurrentDictionary<ulong, Lazy<CleverbotSession>> CleverbotSessions { get; private set; }

        static Chat()
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

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.All, RequireBotTag = false)]
        public async Task Talk([Multiword] string message)
        {
            try
            {
                if (CleverbotSessions.TryGetValue(Context.Guild.Id, out Lazy<CleverbotSession> cleverbot))
                {
                    if (cleverbot.Value.Valid)
                    {
                        await Context.Channel.TriggerTypingAsync().ConfigureAwait(false);
                        var tags = message.ParseDiscordTags();
                        var response = await cleverbot.Value.GetResponseAsync(message).ConfigureAwait(false);
                        await Context.Channel.SendMessageAsync(response.Response).ConfigureAwait(false);
                    }
                    else
                    {
                        await Context.ApplyResultReaction(CommandResult.Failed).ConfigureAwait(false);
                        throw new Exception("Invalid Cleverbot API key.");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.All, DeleteUserMessage = true)]
        public async Task Insult(IUser user)
        {
            var insult = new InsultApi().Insult("");
            if (!string.IsNullOrEmpty(insult?.Insult))
            {
                await Context.Channel.SendMessageAsync(user?.Mention + insult.Insult).ConfigureAwait(false);
            }
            else
            {
                await Context.ApplyResultReaction(CommandResult.Failed).ConfigureAwait(false);
            }
        }
    }
}
