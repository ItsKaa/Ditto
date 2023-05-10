using Cauldron.Core.Collections;
using Discord;
using Discord.Commands;
using Ditto.Attributes;
using Ditto.Bot.Database.Data;
using Ditto.Bot.Database.Models;
using Ditto.Bot.Modules.Admin;
using Ditto.Data.Commands;
using Ditto.Data.Discord;
using Ditto.Extensions;
using Ditto.Extensions.Discord;
using Ditto.Helpers;
using SauceNET;
using SauceNET.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Ditto.Bot.Modules.Utility.Linking
{
    [Alias("sauce")]
    public class SauceNaoLinkUtility : DiscordModule<LinkUtility>
    {
        private static ConcurrentList<Link> Links { get; set; } = new ConcurrentList<Link>();
        private static ConcurrentQueue<IUserMessage> MessageQueue { get; set; } = new ConcurrentQueue<IUserMessage>();
        private static bool Running { get; set; } = false;
        private static bool Initialized { get; set; } = false;
        private static TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(20);
        private static IWebProxy Proxy { get; set; } = new WebProxy();

        private const double SimilarityTolerance = 25;
        private const double DefaultMinSimilarity = 50;

        static SauceNaoLinkUtility()
        {
            Ditto.Initialised += () =>
            {
                if (string.IsNullOrEmpty(Ditto.Settings.Credentials.SauceNaoApiKey))
                {
                    Log.Warn("SauceNAO API key not defined, skipping initialization.");
                    return Task.CompletedTask;
                }

                if (Ditto.Settings.ProxySettings.Enabled)
                {
                    Proxy = new WebProxy(Ditto.Settings.ProxySettings.Host, Ditto.Settings.ProxySettings.Port);
                    if (!string.IsNullOrEmpty(Ditto.Settings.ProxySettings.Username))
                    {
                        Proxy.Credentials = new NetworkCredential(Ditto.Settings.ProxySettings.Username, Ditto.Settings.ProxySettings.Password);
                    }
                }

                Running = true;

                Task.Run(async () =>
                {
                    while(Running)
                    {
                        if (Initialized && MessageQueue.TryDequeue(out IUserMessage message))
                        {
                            await GetSauceAndPostResponse(message).ConfigureAwait(false);
                        }
                        await Task.Delay(Timeout).ConfigureAwait(false);
                    }
                });

                return Task.CompletedTask;
            };

            Ditto.Connected += () =>
            {
                Task.Run(async () =>
                {
                    Initialized = false;

                    Links.Clear();
                    Links.AddRange(await Ditto.Database.ReadAsync(uow => uow.Links.GetAllWithLinks(l => l.Type == LinkType.SauceNAO)).ConfigureAwait(false));

                    await Ditto.Client.DoAsync(c =>
                    {
                        c.MessageReceived -= Ditto_MessageReceived;
                        c.MessageReceived += Ditto_MessageReceived;
                    }).ConfigureAwait(false);

                    Initialized = true;
                });

                return Task.CompletedTask;
            };

            Ditto.Exit += () =>
            {
                Initialized = false;
                return Task.CompletedTask;
            };

            // Empty link handler since it's a one time only configuration
            LinkUtility.TryAddHandler(LinkType.SauceNAO, (link, channel, cancellationToken) => Task.FromResult(Enumerable.Empty<string>()));
        }

        private static Task Ditto_MessageReceived(Discord.WebSocket.SocketMessage socketMessage)
        {
            if (!Running || !(socketMessage is IUserMessage message) || message.Channel == null || message.Author.IsBot)
                return Task.CompletedTask;

            foreach (var link in Links.ToList().Where(x => x.Channel == message.Channel))
            {
                if (link == null || link.Channel == null)
                    continue;

                if (message.Attachments.Any(x => x.ContentType.Contains("image")))
                {
                    MessageQueue.Enqueue(message);
                }
            }

            return Task.CompletedTask;
        }

        [DiscordCommand(CommandSourceLevel.Guild, CommandAccessLevel.Local)]
        [Alias("add", "link", "hook", "register")]
        public async Task Add(ITextChannel channel)
        {
            if (!Permissions.IsAdministratorOrBotOwner(Context))
            {
                await Context.ApplyResultReaction(CommandResult.FailedUserPermission).ConfigureAwait(false);
            }
            else if (channel == null || !await Permissions.CanBotSendMessages(channel).ConfigureAwait(false))
            {
                await Context.ApplyResultReaction(CommandResult.FailedBotPermission).ConfigureAwait(false);
            }
            else
            {
                var link = await LinkUtility.TryAddLinkAsync(LinkType.SauceNAO, channel, $"{DefaultMinSimilarity}", null);
                if (link == null)
                {
                    await Context.ApplyResultReaction(CommandResult.Failed).ConfigureAwait(false);
                }
                else
                {
                    Links.Add(link);
                    await Context.ApplyResultReaction(CommandResult.Success).ConfigureAwait(false);
                }
            }
        }

        [DiscordCommand(CommandSourceLevel.Guild, CommandAccessLevel.Local)]
        [Alias("add", "link", "hook", "register")]
        public async Task Similarity(int similarity)
        {
            var link = Links.FirstOrDefault(x => x.Channel == Context.TextChannel);
            if (!Permissions.IsAdministratorOrBotOwner(Context))
            {
                await Context.ApplyResultReaction(CommandResult.InvalidParameters).ConfigureAwait(false);
            }
            else if (link == null)
            {
                await Context.ApplyResultReaction(CommandResult.InvalidParameters).ConfigureAwait(false);
            }
            else
            {
                Links.Remove(link);
                Links.Add(await Ditto.Database.DoAsync(x =>
                {
                    var dbLink = x.Links.Get(link.Id);
                    dbLink.Value = $"{similarity}";
                    x.Links.Update(dbLink);
                    return dbLink;
                }).ConfigureAwait(false));

                await Context.ApplyResultReaction(CommandResult.Success).ConfigureAwait(false);
            }
        }

        private static async Task<Sauce> GetSauce(string imageUrl)
        {
            using var handler = new HttpClientHandler()
            {
                Proxy = Proxy,
                UseProxy = true,
            };

            using var client = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://saucenao.com")
            };

            var values = new Dictionary<string, string>
            {
                { "db", "999" },
                { "output_type", "2" },
                { "api_key", Ditto.Settings.Credentials.SauceNaoApiKey},
                { "url", imageUrl },
                { "numres", "6" }
            };

            var content = new FormUrlEncodedContent(values);
            var httpResult = await client.PostAsync("search.php", content).ConfigureAwait(false);
            var stringResult = await httpResult.ReadContentAsString().ConfigureAwait(false);
            var sauce = await SauceNETUtil.ParseSauceAsync(stringResult).ConfigureAwait(false);
            return sauce;
        }

        private static string GetPreferredSauceNameWithUrl(IMessage message, IEnumerable<Result> sauce)
        {
            var sauceWithSimilarity = sauce.Select(x => {
                double.TryParse(x.Similarity, out double similarity);
                var title = x.Properties.FirstOrDefault(x => x.Name == "Title")?.Value;
                var pixivId = x.Properties.FirstOrDefault(x => x.Name == "PixivId")?.Value;
                return new { Sauce = x, Similarity = similarity, Title = title, PixivId = pixivId };
            }).OrderByDescending(x => x.Similarity);

            var sauceWithUrls = sauceWithSimilarity.Where(x => !string.IsNullOrEmpty(x.Sauce.SourceURL));
            if (sauceWithUrls.Any(x =>
                message.Content.Contains(x.Sauce.SourceURL)
                || (!string.IsNullOrEmpty(x.PixivId) && message.Content.Contains(x.PixivId))
               ))
            {
                // User already linked sauce
                return "";
            }
            else if (sauceWithUrls.Any())
            {
                var highestSimilarity = sauceWithUrls.Max(X => X.Similarity);
                var link = Links.FirstOrDefault(x => x.Channel == message.Channel);
                if (double.TryParse(link?.Value, out double minimumSimilarity)
                    && highestSimilarity < minimumSimilarity)
                {
                    // Similarity too low.
                    return "";
                }

                // 1. Pixiv
                var saucePixiv = sauceWithUrls.FirstOrDefault(x => x.Sauce.SourceURL.Contains("pixiv.net"));
                if (saucePixiv != null && saucePixiv.Similarity > (highestSimilarity - SimilarityTolerance))
                {
                    var pixivUrl = saucePixiv.PixivId != null ? $"https://www.pixiv.net/en/artworks/{saucePixiv.PixivId}" : saucePixiv.Sauce.SourceURL;
                    if (saucePixiv.Title == null)
                        return pixivUrl;
                    else
                        return $"{pixivUrl} ({saucePixiv.Title})";
                }

                // 2. Twitter
                var sauceTwitter = sauceWithUrls.FirstOrDefault(x => x.Sauce.SourceURL.Contains("twitter.com"));
                if (sauceTwitter != null && sauceTwitter.Similarity > (highestSimilarity - SimilarityTolerance))
                {
                    var twitterUrl = sauceTwitter.Sauce.SourceURL;
                    if (sauceTwitter.Title == null)
                        return twitterUrl;
                    else
                        return $"{twitterUrl} ({sauceTwitter.Title})";
                }

                // 3. Others
                return sauceWithUrls.FirstOrDefault().Sauce.SourceURL;
            }

            var firstSauce = sauceWithSimilarity.FirstOrDefault()?.Sauce;
            return firstSauce != null ? $"{firstSauce.Name}: <no url>" : "";
        }

        private static async Task GetSauceAndPostResponse(IUserMessage message)
        {
            // Check number of links compared to the number of attachments.
            var matches = Globals.RegularExpression.Urls.Matches(message.Content);
            if (matches.Any(x => x.Success) && matches.Count >= message.Attachments.Count)
            {
                return;
            }

            var description = "";
            foreach (var attachment in message.Attachments)
            {
                if (message.Attachments.Count > 1)
                {
                    await Task.Delay(Timeout).ConfigureAwait(false);
                }

                var sauce = await GetSauce(attachment.Url).ConfigureAwait(false);
                var sauceText = GetPreferredSauceNameWithUrl(message, sauce.Results);
                if (!string.IsNullOrEmpty(sauceText))
                {
                    description += $"{sauceText}\n";
                }
            }

            if (string.IsNullOrEmpty(description))
            {
                await message.AddReactionsAsync(Emotes.GreyQuestion).ConfigureAwait(false);
            }
            else
            {
                var embed = new EmbedBuilder()
                    .WithAuthor("🎨 Sauce")
                    .WithOkColour((message.Channel as IGuildChannel)?.Guild)
                    .WithDescription(description);

                try
                {
                    await message.ReplyAsync(
                        embed: embed.Build(),
                        allowedMentions: AllowedMentions.None,
                        options: new RequestOptions() { RetryMode = RetryMode.AlwaysRetry }
                    ).ConfigureAwait(false);
                }
                catch { }
            }
        }
    }
}
