using Discord;
using Discord.Commands;
using Ditto.Attributes;
using Ditto.Bot.Data.API.Rest;
using Ditto.Bot.Modules.Admin;
using Ditto.Data.Chatting;
using Ditto.Data.Commands;
using Ditto.Data.Discord;
using Ditto.Extensions;
using Ditto.Helpers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
                        () => new CleverbotSession(Ditto.Settings.Credentials.CleverbotApiKey, false), true)
                    );
                }
                return Task.CompletedTask;
            });

            Ditto.Client.Do((c) => c.LeftGuild += (guild) =>
            {
                if (guild != null)
                {
                    CleverbotSessions.TryRemove(guild.Id, out Lazy<CleverbotSession> session);
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

        [DiscordCommand(CommandSourceLevel.Guild, CommandAccessLevel.All)]
        public async Task Prune(int count = 100, IUser user = null, [Multiword] string pattern = null)
        {
            if (!Permissions.IsAdministratorOrBotOwner(Context))
            {
                await Context.ApplyResultReaction(CommandResult.FailedUserPermission).ConfigureAwait(false);
                return;
            }

            if (count > 10)
            {
                var usedReaction = await Context.SendOptionDialogueAsync(
                    $"{Context.User.Mention} Please verify. Do you wish to delete {count} messages from this channel?",
                    new List<string>(), true,
                    new[] { EmotesHelper.GetEmoji(Emotes.WhiteCheckMark), EmotesHelper.GetEmoji(Emotes.NoEntrySign) }, null, 60000, 0
                ).ConfigureAwait(false);
                if(usedReaction != 1)
                {
                    return;
                }
            }

            if (count > 100)
                count = 100;
            else if (count <= 0)
                count = 1;

            var list = (await Context.Channel.GetMessagesAsync(count).ToListAsync())
                .SelectMany(i => i);

            if (!string.IsNullOrEmpty(pattern))
            {
                list = list.Where(i => i.Content.Contains(pattern));
            }

            if(user != null)
            {
                list = list.Where(i => i.Author?.Id == user.Id);
            }

            foreach (var msg in list)
            {
                try
                {
                    if (!msg.IsPinned)
                    {
                        await msg.DeleteAsync().ConfigureAwait(false);
                    }
                }
                catch { }
            }
        }


        [Priority(4), DiscordCommand(CommandSourceLevel.Guild, CommandAccessLevel.All)]
        [Help(null, "Make the bot user send a message.")]
        public async Task Say(
            [Help("channel", "The targeted text channel", optional: true)]
            ITextChannel channel,
            [Help("user", "The user to mention", optional: true)]
            IUser user,
            [Help("message", "The message to write")]
            [Multiword] string message)
        {
            if (!Permissions.IsAdministratorOrBotOwner(Context))
            {
                await Context.ApplyResultReaction(CommandResult.FailedUserPermission).ConfigureAwait(false);
                return;
            }

            // Check the channel
            if (channel == null)
            {
                channel = Context.TextChannel;
                if(channel == null)
                {
                    await Context.ApplyResultReaction(CommandResult.Failed).ConfigureAwait(false);
                    return;
                }
            }

            // Check user permissions
            var guildUserPermissions = Context.GuildUser?.GetPermissions(channel);
            if(guildUserPermissions?.ViewChannel != true || guildUserPermissions?.SendMessages != true)
            {
                await Context.ApplyResultReaction(CommandResult.FailedUserPermission).ConfigureAwait(false);
                return;
            }

            // Check bot permissions
            var channelPermissions = await Ditto.Client.DoAsync(c => c.GetPermissionsAsync(channel)).ConfigureAwait(false);
            if (!channelPermissions.ViewChannel || !channelPermissions.SendMessages)
            {
                await Context.ApplyResultReaction(CommandResult.FailedBotPermission).ConfigureAwait(false);
                return;
            }

            await channel.SendMessageAsync((user == null ? string.Empty : $"{user?.Mention} ") + message).ConfigureAwait(false);
        }

        [Priority(3), DiscordCommand(CommandSourceLevel.Guild, CommandAccessLevel.All)]
        public Task Say(IUser user, ITextChannel channel, [Multiword] string message)
            => Say(channel, user, message);

        [Priority(2), DiscordCommand(CommandSourceLevel.Guild, CommandAccessLevel.All)]
        public Task Say(IUser user, [Multiword] string message)
            => Say(null, user, message);

        [Priority(1), DiscordCommand(CommandSourceLevel.Guild, CommandAccessLevel.All)]
        public Task Say(ITextChannel channel, [Multiword] string message)
            => Say(channel, null, message);

        [Priority(0), DiscordCommand(CommandSourceLevel.Guild, CommandAccessLevel.All)]
        public Task Say([Multiword] string message)
            => Say((ITextChannel)null, null, message);
    }
}
