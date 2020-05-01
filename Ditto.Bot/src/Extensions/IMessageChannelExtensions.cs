using Discord;
using Discord.WebSocket;
using Ditto.Bot;
using Ditto.Data.Commands;
using Ditto.Helpers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ditto.Extensions
{
    public static class IMessageChannelExtensions
    {
        public static Task<IUserMessage> EmbedAsync(this IMessageChannel channel, string message, IGuild guild = null, ContextMessageOption options = ContextMessageOption.None, RetryMode retryMode = RetryMode.AlwaysRetry)
            => EmbedAsync(channel, message, null, guild, options, retryMode: retryMode);

        public static Task<IUserMessage> EmbedAsync(this IMessageChannel channel, string message, EmbedBuilder embedBuilder, IGuild guild = null, ContextMessageOption options = ContextMessageOption.None, IUser replyUser = null, RetryMode retryMode = RetryMode.AlwaysRetry)
        {
            if (false == embedBuilder?.Color.HasValue)
            {
                embedBuilder = embedBuilder.WithOkColour(guild);
            }

            var error = options.Has(ContextMessageOption.Error);
            var replyToUser = options.Has(ContextMessageOption.ReplyUser);

            return channel.EmbedAsync
                (
                embedBuilder ?? new EmbedBuilder()
                {
                    Description = string.Format("{0}{1}{2}",
                        error ? "💢 " : "",
                        replyToUser ? $"{replyUser?.Mention} " : "",
                        message
                    ),
                    Color = (error ? Bot.Ditto.Cache.Db.EmbedErrorColour(guild)
                        : Bot.Ditto.Cache.Db.EmbedColour(guild)
                    )
                },
                embedBuilder == null ? string.Empty : message,
                new RequestOptions() { RetryMode = retryMode }
            );
        }

        public static Task<IUserMessage> EmbedAsync(this IMessageChannel channel, IGuild guild, string format, params object[] args)
             => EmbedAsync(channel, string.Format(format, args), guild);

        public static Task<IUserMessage> EmbedAsync(this IMessageChannel channel, ContextMessageOption options, IGuild guild, string format, params object[] args)
            => EmbedAsync(channel, string.Format(format, args), guild, options);

        public static Task<IUserMessage> EmbedAsync(this IMessageChannel channel, EmbedBuilder embedBuilder, IGuild guild, ContextMessageOption options = ContextMessageOption.None, RetryMode retryMode = RetryMode.AlwaysRetry)
            => EmbedAsync(channel, "", embedBuilder, guild, options, retryMode: retryMode);






        // Maximum amount of reactions=20
        private static ConcurrentQueue<IEmote> _OptionsEmoji = new ConcurrentQueue<IEmote>(new[] {
            new Emoji("1⃣"),
            new Emoji("2⃣"),
            new Emoji("3⃣"),
            new Emoji("4⃣"),
            new Emoji("5⃣"),
            new Emoji("6⃣"),
            new Emoji("7⃣"),
            new Emoji("8⃣"),
            new Emoji("9⃣"),
            new Emoji("🔟"),
            new Emoji("🇦"),
            new Emoji("🇧"),
            new Emoji("🇨"),
            new Emoji("🇩"),
            new Emoji("🇪"),
            new Emoji("🇫"),
            new Emoji("🇬"),
            new Emoji("🇭"),
            new Emoji("🇮"),
            new Emoji("🇯"),
        });

        private static ConcurrentQueue<IEmote> _optionsEmojiPaging = new ConcurrentQueue<IEmote>(new[] {
            new Emoji("⬅"),
            new Emoji("➡")
        });

        
        public static async Task<int> SendOptionDialogueAsync(this IMessageChannel channel,
            string headerMessage,
            IEnumerable<string> options,
            IDiscordClient discordClient,
            bool awaitSingleReaction,
            IEnumerable<IEmote> reactionEmotes = null,
            Action<int, string> onReaction = null,
            int timeout = 30000,
            int addedTimeoutOnReaction = 30000
            )
        {
            int selection = -1;
            int seconds = timeout / 1000;
            if(reactionEmotes == null || reactionEmotes.Count() <= 0)
            {
                reactionEmotes = _OptionsEmoji;
            }

            var embedFields = new List<EmbedFieldBuilder>();
            if (options != null)
            {
                for (int i = 0; i < options.Count(); i++)
                {
                    embedFields.Add(new EmbedFieldBuilder()
                    {
                        //Name = string.Format("__**`{0}`**__", i+1),
                        Name = reactionEmotes.ElementAt(i).Name,
                        Value = options.ElementAt(i),
                        IsInline = true
                    });
                }
            }

            var embedBuilder = new EmbedBuilder()
            {
                Description = headerMessage,
                Footer = new EmbedFooterBuilder() { Text = string.Format("{0} {1} seconds left", Globals.Character.Clock, seconds) },
                Color = Bot.Ditto.Cache.Db.EmbedColour((channel as ITextChannel)?.Guild)
            };

            if(embedFields.Count > 0)
            {
                embedBuilder.Fields = embedFields;
                embedBuilder.Description = string.Format("{0}\n{1}\n", headerMessage, Globals.Character.HiddenSpace);
            }

            var message = await channel.EmbedAsync(embedBuilder);

            foreach(var emote in reactionEmotes)
            {
                await Task.Run(() =>
                {
                    message.AddReactionAsync(emote, DiscordHelper.GetRequestOptions(true).SetRetryMode(RetryMode.AlwaysRetry));
                }).ConfigureAwait(false);
            }

            var tokenSource = new CancellationTokenSource();
            Bot.Ditto.ReactionHandler.Add(message, (r) =>
            {
                if (r.UserId != discordClient.CurrentUser.Id)
                {
                    var option = reactionEmotes.Select((Value, Index) => new { Value, Index }).FirstOrDefault(a => a.Value.Name == r.Emote.Name);
                    if (option != null && option.Value != null)
                    {
                        selection = option.Index + 1;
                        onReaction?.Invoke(option.Index, option.Value.Name);
                        seconds += addedTimeoutOnReaction / 1000;
                    }
                    if (awaitSingleReaction)
                    {
                        tokenSource.Cancel();
                    }
                }
                return Task.CompletedTask;
            });

            try
            {
                await Task.Run(async () =>
                {
                    while (seconds > 0)
                    {
                        await Task.Run(() =>
                        {
                            message.ModifyAsync((m) =>
                            {
                                embedBuilder.Footer.Text = string.Format("{0} {1} second{2} left", Globals.Character.Clock, seconds--, seconds > 1 ? "s" : "");
                                m.Embed = new Optional<Embed>(embedBuilder.Build());
                            });
                        }, tokenSource.Token).ConfigureAwait(false);
                        await Task.Delay(1000, tokenSource.Token).ConfigureAwait(false);
                    }
                }, tokenSource.Token);
            }
            catch (OperationCanceledException) { }
            
            var __ = Task.Run(async () =>
            {
                await Task.Delay(1000).ConfigureAwait(false);
                await message.DeleteAsync().ConfigureAwait(false);
            });

            Bot.Ditto.ReactionHandler.Remove(message);
            return selection;
        }


        public static async Task SendListDialogue(this IMessageChannel channel, string headerMessage, string[] options, IDiscordClient discordClient, int timeout = 30000)
        {
            int seconds = timeout / 1000;
            //var embedFields = new List<EmbedFieldBuilder>();
            string description = "";

            if (options != null)
            {
                for (int i = 0; i < options.Length; i++)
                {
                    /*
                    embedFields.Add(new EmbedFieldBuilder()
                    {
                        //Name = string.Format("__**`{0}`**__", i+1),
                        Name =  "`" + i.ToString() + "`",
                        Value = options[i],
                        IsInline = false
                    });
                    */
                    description += $"`{i}` {options[i]}\n\n";
                }
            }

            var embedBuilder = new EmbedBuilder()
            {
                Title = string.Format("{0}\n{1}\n", headerMessage, Globals.Character.HiddenSpace),
                //Fields = embedFields,
                Description = description,
                Footer = new EmbedFooterBuilder() { Text = string.Format("{0} {1} seconds left", Globals.Character.Clock, seconds) },
                Color = Bot.Ditto.Cache.Db.EmbedColour((channel as ITextChannel)?.Guild)
            };
            var message = await channel.EmbedAsync(embedBuilder);

            var tokenSource = new CancellationTokenSource();

            try
            {
                await Task.Run(async () =>
                {
                    while (seconds > 0)
                    {
                        await Task.Run(() =>
                        {
                            message.ModifyAsync((m) =>
                            {
                                embedBuilder.Footer.Text = string.Format("{0} {1} second{2} left", Globals.Character.Clock, seconds--, seconds > 1 ? "s" : "");
                                m.Embed = new Optional<Embed>(embedBuilder.Build());
                            });
                        }).ConfigureAwait(false);
                        await Task.Delay(1000, tokenSource.Token).ConfigureAwait(false);
                    }
                }, tokenSource.Token);
            }
            catch (TaskCanceledException) { }
            //try { await Task.Delay(timeout, tokenSource.Token).ConfigureAwait(false); } catch(TaskCanceledException) { }

            await Task.Run(() =>
            {
                Task.Run(async () =>
                {
                    await Task.Delay(1000).ConfigureAwait(false);
                    await message.DeleteAsync().ConfigureAwait(false);
                });
            }).ConfigureAwait(false);
        }
        public static Task SendListDialogue(this IMessageChannel channel, string headerMessage, IEnumerable<string> options, IDiscordClient discordClient, int timeout = 30000)
            => SendListDialogue(channel, headerMessage, options?.ToArray(), discordClient, timeout);


        public static async Task SendPagedMessageAsync(this IMessageChannel channel,
            IDiscordClient discordClient,
            EmbedBuilder embedBuilder,
            Func<IUserMessage, int, EmbedBuilder> onPageChange, // message, page
            int pageCount = int.MaxValue,
            bool displayPage = true,
            int timeout = 30000,
            int addedTimeoutOnReaction = 30000)
        {
            int page = 1;
            var reactionEmotes = _optionsEmojiPaging;
            DateTime expireDate = DateTime.Now + TimeSpan.FromMilliseconds(timeout);
            var tokenSource = new CancellationTokenSource();

            embedBuilder.WithFooter(new EmbedFooterBuilder().WithText($"{Globals.Character.Clock} expires at {expireDate:t} | {page}/{pageCount}"));
            embedBuilder.Color = Bot.Ditto.Cache.Db.EmbedColour((channel as ITextChannel)?.Guild);
            var message = await channel.EmbedAsync(embedBuilder).ConfigureAwait(false);

            foreach(var reaction in reactionEmotes)
            {
                await Task.Run(() =>
                {
                    message.AddReactionAsync(reaction,
                        DiscordHelper.GetRequestOptions(true).SetRetryMode(RetryMode.AlwaysRetry)
                    );
                }).ConfigureAwait(false);
            }

            // onChanged event, either for adding or removing a reaction
            var onChanged = new Func<SocketReaction, Task>(async r =>
            {
                if (r.UserId != discordClient.CurrentUser.Id)
                {
                    var option = reactionEmotes.Select((Value, Index) => new { Value, Index }).FirstOrDefault(a => a.Value.Name == r.Emote.Name);
                    if (option != null && option.Value != null)
                    {
                        var execute = false;
                        if(option.Value == _optionsEmojiPaging.ElementAt(0) && page > 1)
                        {
                            execute = true;
                            page--;
                        }
                        else if(option.Value == _optionsEmojiPaging.ElementAt(1) && page < pageCount)
                        {
                            execute = true;
                            page++;
                        }
                        
                        expireDate += TimeSpan.FromMilliseconds(addedTimeoutOnReaction);
                        if (execute)
                        {
                            embedBuilder = onPageChange?.Invoke(message, page);
                            embedBuilder.WithFooter(new EmbedFooterBuilder().WithText($"{Globals.Character.Clock} expires at {expireDate:t}{(displayPage ? $" | {page}/{pageCount}" : "")}"));
                            embedBuilder.Color = Bot.Ditto.Cache.Db.EmbedColour((channel as ITextChannel)?.Guild);
                            try
                            {
                                await message.ModifyAsync((m) => m.Embed = new Optional<Embed>(embedBuilder.Build()),
                                    new RequestOptions() { RetryMode = RetryMode.AlwaysRetry, Timeout = 500 }
                                ).ConfigureAwait(false);
                            }
                            catch { }
                        }
                    }
                }
            });
            Bot.Ditto.ReactionHandler.Add(message, onChanged, onChanged, null);

            // Wait until date >= expireDate
            try
            {
                await Task.Run(async () =>
                {
                    while(DateTime.Now < expireDate)
                    {
                        await Task.Delay(500, tokenSource.Token).ConfigureAwait(false);
                    }
                }, tokenSource.Token);
            }
            catch (OperationCanceledException) { }
            
            // Clear reactions and remove the footer
            Bot.Ditto.ReactionHandler.Remove(message);
            await message.RemoveAllReactionsAsync().ConfigureAwait(false);
            try
            {
                await message.ModifyAsync((m) =>
                {
                    embedBuilder.Footer = new EmbedFooterBuilder();
                    m.Embed = new Optional<Embed>(embedBuilder.Build());
                },
                new RequestOptions() { RetryMode = RetryMode.AlwaysRetry }).ConfigureAwait(false);
            }
            catch { }
        }
    }
}
