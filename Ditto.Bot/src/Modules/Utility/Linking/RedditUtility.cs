using Discord;
using Discord.Commands;
using Ditto.Attributes;
using Ditto.Bot.Database.Data;
using Ditto.Data.Commands;
using Ditto.Data.Discord;
using Ditto.Extensions;
using Ditto.Helpers;
using RedditSharp;
using RedditSharp.Things;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Ditto.Bot.Modules.Utility.Linking
{
    [Alias("reddit")]
    public class RedditUtility : DiscordModule<LinkUtility>
    {
        //private static Reddit _reddit = new Reddit(RateLimitMode.Burst, true);
        private static Reddit _reddit = new Reddit(RateLimitMode.None, true);
        private static DateTime _date = DateTime.MinValue;
        private static TimeSpan _interval = TimeSpan.FromSeconds(2);
        private static SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

        static RedditUtility()
        {
            LinkUtility.TryAddHandler(LinkType.Reddit, async (link, channel, cancellationToken) =>
            {
                var listUrls = new List<string>();
                
                var retryCount = 0;
                var sub = await DoAsync(r => r.GetSubredditAsync(link.Value)).ConfigureAwait(false);
                var posts = await sub.GetPosts(Subreddit.Sort.New, -1)
                    .Where(i => !LinkUtility.LinkItemExists(link, i.Id, StringComparison.CurrentCultureIgnoreCase))
                    .Where(i => i.CreatedUTC > link.Date.ToUniversalTime())
                    .Where(i => !i.Url.LocalPath.EndsWith(".py")) // avoid virusses
                    .Reverse()
                    .ToListAsync();

                for (int i = 0; i < posts.Count; i++)
                {
                    var post = posts[i];
                    Log.Info($"Posting \"{post.Title}\" ({LinkType.Reddit.ToString()}) in \"{channel.Guild.Name}:{channel.Name}\"");
                    try
                    {
                        if(cancellationToken.IsCancellationRequested)
                        {
                            return listUrls;
                        }

                        await channel.EmbedAsync(ParseEmbed(post, sub, channel.Guild),
                            options: new RequestOptions() { RetryMode = RetryMode.AlwaysFail }
                        ).ConfigureAwait(false);
                        retryCount = 0;
                    }
                    catch
                    {
                        if (retryCount < 3)
                        {
                            i--;
                            Log.Warn($"Attempting to resend ({++retryCount}/3)...");
                            await Task.Delay(2000).ConfigureAwait(false);
                        }
                        else
                        {
                            Log.Warn($"Reached the maximum amount of attempts, aborting.");
                            retryCount = 0;
                        }
                    }
                    listUrls.Add(post.Id);
                    await Task.Delay(500).ConfigureAwait(false);
                }
                return listUrls;
            });
        }

        private static EmbedBuilder ParseEmbed(Post post, Subreddit subreddit, IGuild guild)
        {
            var htmlCode = WebHelper.GetSourceCode(post.Shortlink);
            var metaInfo = WebHelper.GetMetaInfoFromHtml(htmlCode);
            //var siteName = metaInfo.FirstOrDefault(e => e.Name.Equals("og:site_name", StringComparison.CurrentCultureIgnoreCase))?.Value;
            var title = metaInfo.FirstOrDefault(e => e.Name.Equals("title", StringComparison.CurrentCultureIgnoreCase))?.Value
                ?? metaInfo.FirstOrDefault(e => e.Name.Equals("og:title", StringComparison.CurrentCultureIgnoreCase))?.Value
                ?? WebHelper.GetTitleFromHtml(htmlCode);
            var description = metaInfo.FirstOrDefault(e => e.Name.Equals("description", StringComparison.CurrentCultureIgnoreCase))?.Value
                ?? metaInfo.FirstOrDefault(e => e.Name.Equals("og:description", StringComparison.CurrentCultureIgnoreCase))?.Value
                ?? "";
            var image = metaInfo.FirstOrDefault(e => e.Name.Equals("og:image", StringComparison.CurrentCultureIgnoreCase))?.Value;


            var linkHtmlCode = WebHelper.GetSourceCode(post.Url.ToString());
            var linkMeta = WebHelper.GetMetaInfoFromHtml(linkHtmlCode);
            var linkTitle = linkMeta.FirstOrDefault(e => e.Name.Equals("title", StringComparison.CurrentCultureIgnoreCase))?.Value
                ?? linkMeta.FirstOrDefault(e => e.Name.Equals("og:title", StringComparison.CurrentCultureIgnoreCase))?.Value
                ?? WebHelper.GetTitleFromHtml(linkHtmlCode);
            var linkDescription = linkMeta.FirstOrDefault(e => e.Name.Equals("description", StringComparison.CurrentCultureIgnoreCase))?.Value
                ?? linkMeta.FirstOrDefault(e => e.Name.Equals("og:description", StringComparison.CurrentCultureIgnoreCase))?.Value
                ?? "";
            var linkImage = linkMeta.FirstOrDefault(e => e.Name.Equals("og:image", StringComparison.CurrentCultureIgnoreCase))?.Value;

            var date = post.CreatedUTC;
            return new EmbedBuilder()
                .WithAuthor(new EmbedAuthorBuilder()
                    .WithName(WebUtility.HtmlDecode(title))
                    //.WithIconUrl("https://www.redditstatic.com/favicon.ico")
                    .WithIconUrl("https://www.redditstatic.com/icon-touch.png")
                    .WithUrl(post.Shortlink)
                )
                .WithTitle(post.Permalink.Equals(post.Url.LocalPath) ? null : WebUtility.HtmlDecode(linkTitle))
                .WithThumbnailUrl(
                    WebHelper.IsValidWebsite(post.Thumbnail?.ToString())
                    ? post.Thumbnail.ToString()
                    : image
                )
                .WithDescription(WebUtility.HtmlDecode(description))
                .WithFooter(new EmbedFooterBuilder()
                    .WithText($"⏰ posted at {date:dddd, MMMM} {date.Day.Ordinal()} {date:yyyy HH:mm}")
                )
                .WithRssColour(guild)
                .WithUrl(post.Url.ToString());
        }

        [DiscordCommand(CommandSourceLevel.Guild, CommandAccessLevel.Local)]
        public Task _(ITextChannel textChannel, [Multiword] string url, DateTime? fromDate = null)
            => Add(textChannel, url, fromDate);

        [DiscordCommand(CommandSourceLevel.Guild, CommandAccessLevel.Local)]
        [Alias("add", "link", "hook", "register")]
        public async Task Add(ITextChannel textChannel, [Multiword] string url, DateTime? fromDate = null)
        {
            if (!(await Ditto.Client.DoAsync(
                    c => c.GetPermissionsAsync(textChannel)
                ).ConfigureAwait(false)).HasAccess())
            {
                await Context.ApplyResultReaction(CommandResult.FailedBotPermission).ConfigureAwait(false);
                return;
            }
            //else if (!WebHelper.IsValidWebsite(url))
            //{
            //    await Context.EmbedAsync(
            //        $"Could not parse that url.",
            //        ContextMessageOption.ReplyWithError
            //    ).ConfigureAwait(false);
            //}
            else
            {
                // Check for an existing item
                var subRedditLink = "";
                var redditError = false;
                try
                {
                    var uri = WebHelper.ToUri(url);

                    var search = await DoAsync(r => r.GetSubredditAsync(uri?.LocalPath ?? url)).ConfigureAwait(false);
                    subRedditLink = search?.Url.ToString();
                    if (search == null || string.IsNullOrEmpty(search.Title))
                    {
                        redditError = true;
                    }
                    else
                    {
                    }
                }
                catch {
                    redditError = true;
                }

                if (redditError)
                {
                    await Context.ApplyResultReaction(CommandResult.Failed).ConfigureAwait(false);
                    return;
                }

                if (null == await LinkUtility.TryAddLinkAsync(LinkType.Reddit, textChannel, subRedditLink, fromDate))
                {
                    await Context.ApplyResultReaction(CommandResult.Failed).ConfigureAwait(false);
                    await Context.EmbedAsync(
                        $"That url is already linked to {textChannel.Mention}",
                        ContextMessageOption.ReplyWithError
                    ).ConfigureAwait(false);
                    return;
                }
            }
        }


        private static Task<TResult> DoAsync<TResult>(Func<Reddit, Task<TResult>> func)
        {
            return _semaphoreSlim.DoAsync(async () =>
            {
                if ((_date + _interval) < DateTime.Now) { }
                else
                {
                    var delay = _interval - (DateTime.Now - _date);
                    await Task.Delay(delay).ConfigureAwait(false);
                }
                _date = DateTime.Now;
                return await func(_reddit);
            });
        }
    }
}