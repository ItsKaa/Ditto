using Discord;
using Ditto.Bot;
using Ditto.Helpers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ditto.Extensions
{
    public static class IUserMessageExtensions
    {
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
        
        public static async Task<int> SendOptionDialogueAsync(this IMessageChannel channel, string headerMessage, string[] options, IDiscordClient discordClient, int timeout = 30000)
        {
            int selection = -1;
            int seconds = timeout / 1000;
            var embedFields = new List<EmbedFieldBuilder>();

            for (int i = 0; i < options.Length; i++)
            {
                embedFields.Add(new EmbedFieldBuilder()
                {
                    //Name = string.Format("__**`{0}`**__", i+1),
                    Name = _OptionsEmoji.ElementAt(i).Name,
                    Value = options[i],
                    IsInline = true
                });
            }

            var embedBuilder = new EmbedBuilder()
            {
                Description = string.Format("{0}\n{1}\n", headerMessage, Globals.Character.HiddenSpace),
                Fields = embedFields,
                Footer = new EmbedFooterBuilder() { Text = string.Format("{0} {1} seconds left", Globals.Character.Clock, seconds) },
                Color = Bot.Ditto.Cache.Db.EmbedColour((channel as ITextChannel)?.Guild)
            };
            var message = await channel.EmbedAsync(embedBuilder);

            for (int i = 0; i < options.Length; i++)
            {
                if (i > _OptionsEmoji.Count-1)
                    break;
                await Task.Run(() =>
                {
                    message.AddReactionAsync(_OptionsEmoji.ElementAt(i),
                        DiscordHelper.GetRequestOptions(true).SetRetryMode(RetryMode.AlwaysRetry)
                    //new RequestOptions() {
                    //    HeaderOnly = true,
                    //    RetryMode = RetryMode.AlwaysRetry
                    //});
                    );
                }).ConfigureAwait(false);
            }
            
            var tokenSource = new CancellationTokenSource();
            Bot.Ditto.ReactionHandler.Add(message, (r) =>
            {
                if (r.UserId != discordClient.CurrentUser.Id)
                {
                    var option = _OptionsEmoji.Select((Value, Index) => new { Value, Index }).FirstOrDefault(a => a.Value.Name == r.Emote.Name);
                    if (option != null && option.Value != null)
                    {
                        selection = option.Index + 1;
                    }
                    tokenSource.Cancel();
                }
            });
            
            try
            {
                await Task.Run(async () =>
                {
                    while(seconds > 0)
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

            Bot.Ditto.ReactionHandler.Remove(message);
            return selection;
        }
        public static Task<int> SendOptionDialogueAsync(this IMessageChannel channel, string headerMessage, IEnumerable<string> options, IDiscordClient discordClient, int timeout = 30000)
            => SendOptionDialogueAsync(channel, headerMessage, options.ToArray(), discordClient, timeout);

        
        public static async Task SendListDialogue(this IMessageChannel channel, string headerMessage, string[] options, IDiscordClient discordClient, int timeout = 30000)
        {
            int seconds = timeout / 1000;
            //var embedFields = new List<EmbedFieldBuilder>();
            string description = "";

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
            => SendListDialogue(channel, headerMessage, options.ToArray(), discordClient, timeout);


    }
}
