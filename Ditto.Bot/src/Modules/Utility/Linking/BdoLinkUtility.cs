using Discord;
using Discord.Commands;
using Ditto.Attributes;
using Ditto.Bot.Database.Data;
using Ditto.Bot.Services;
using Ditto.Data.Commands;
using Ditto.Data.Discord;
using Ditto.Extensions;
using Ditto.Extensions.Discord;
using Ditto.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ditto.Bot.Modules.Utility.Linking
{
    [Alias("bdo")]
    class BdoLinkUtility : DiscordTextModule<LinkUtility>
    {
        static BdoLinkUtility()
        {
            LinkUtility.TryAddHandler(LinkType.BDO, async (link, channel, cancellationToken) =>
            {
                var listUrls = new List<string>();
                var url = link.Value;
                var feed = FeedHelper.ParseRss(url);
                var items = feed.Items
                    .Where(item => !LinkUtility.LinkItemExists(link, item.Guid, StringComparison.CurrentCultureIgnoreCase))
                    .Where(item => item.PublishDate.ToUniversalTime() > link.Date.ToUniversalTime())
                    .Reverse();

                // Retrieve the last article id
                ulong news_id = 0;
                Ditto.Database.Do((uow) =>
                {
                    ulong.TryParse(uow.Configs.GetBdoNewsIdentifier(channel.Guild).Value, out news_id);
                }, complete: false);

                if (items.Count() > 0)
                {
                    var retryCount = 0;
                    for (int i = 0; i < items.Count(); i++)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            return listUrls;
                        }

                        var item = items.ElementAt(i);
                        if (ulong.TryParse(new string(item.Link.Reverse().Before('.').Reverse().ToArray()).Replace("/", "").Replace("\\", ""), out ulong identifier))
                        {
                            // Skip older posts
                            if(news_id > identifier)
                            {
                                continue;
                            }
                            else
                            {
                                // Add/update the news identifier for this guild
                                Ditto.Database.Do(uow =>
                                {
                                    uow.Configs.SetBdoNewsIdentifier(channel.Guild, identifier);
                                }, complete: true);
                            }
                        }

                        try
                        {
                            Log.Debug($"[BDO] Posting \"{item.Title}\" ({LinkType.BDO.ToString()}) in \"{link.Guild.Name}:{link.Channel.Name}\"");
                            await channel.EmbedAsync(await ParseEmbed(item.Link, channel.Guild, feed.Channel?.Title).ConfigureAwait(false),
                                options: new RequestOptions() { RetryMode = RetryMode.AlwaysFail }
                            );
                            listUrls.Add(item.Guid ?? item.Link);
                            retryCount = 0;
                        }
                        catch (Exception ex)
                        {
                            if (!await LinkUtility.SendRetryLinkMessageAsync(
                                link.Type,
                                retryCount++,
                                ex is Discord.Net.RateLimitedException ? null : ex
                                ))
                            {
                                break;
                            }
                            i--;
                        }
                    }
                }
                return listUrls;
            });
        }

        public BdoLinkUtility(DatabaseCacheService cache, DatabaseService database) : base(cache, database)
        {
        }

        [DiscordCommand(CommandSourceLevel.Guild, CommandAccessLevel.Local)]
        [Alias("add", "link", "hook", "register")]
        public async Task Add(ITextChannel textChannel, [Multiword] string url, DateTime? fromDate = null)
        {
            if (!(await Ditto.Client.GetPermissionsAsync(textChannel)).HasAccess())
            {
                await Context.ApplyResultReaction(CommandResult.FailedBotPermission).ConfigureAwait(false);
            }

            // Only allow using channels of the current guild.
            if (textChannel != null && textChannel.Guild != Context.Guild)
            {
                await Context.ApplyResultReaction(CommandResult.FailedUserPermission).ConfigureAwait(false);
                return;
            }

            else if (!WebHelper.IsValidWebsite(url))
            {
                await Context.ApplyResultReaction(CommandResult.Failed).ConfigureAwait(false);
            }
            else
            {
                // Check for an existing item
                var uri = WebHelper.ToUri(url);
                if (null == await LinkUtility.TryAddLinkAsync(LinkType.BDO, textChannel, uri.ToString(), (left, right) => {
                    return WebHelper.Compare(WebHelper.ToUri(left), WebHelper.ToUri(right));
                }, fromDate))
                {
                    await Context.EmbedAsync(
                        $"That url is already linked to {textChannel.Mention}",
                        ContextMessageOption.ReplyWithError
                    ).ConfigureAwait(false);
                    await Context.ApplyResultReaction(CommandResult.Failed).ConfigureAwait(false);
                    return;
                }
                else
                {
                    await Context.ApplyResultReaction(CommandResult.Success).ConfigureAwait(false);
                }
            }
        }

        protected static async Task<EmbedBuilder> ParseEmbed(string url, IGuild guild, string channelName)
        {
            var htmlCode = await WebHelper.GetSourceCodeAsync(url).ConfigureAwait(false);
            var metaInfo = WebHelper.GetMetaInfoFromHtml(htmlCode);
            var siteName = metaInfo.FirstOrDefault(e => e.Name.Equals("og:site_name", StringComparison.CurrentCultureIgnoreCase))?.Value;
            var title = metaInfo.FirstOrDefault(e => e.Name.Equals("title", StringComparison.CurrentCultureIgnoreCase))?.Value
                ?? metaInfo.FirstOrDefault(e => e.Name.Equals("og:title", StringComparison.CurrentCultureIgnoreCase))?.Value
                ?? WebHelper.GetTitleFromHtml(htmlCode);
            var description = metaInfo.FirstOrDefault(e => e.Name.Equals("description", StringComparison.CurrentCultureIgnoreCase))?.Value
                ?? metaInfo.FirstOrDefault(e => e.Name.Equals("og:description", StringComparison.CurrentCultureIgnoreCase))?.Value
                ?? "";
            var image = metaInfo.FirstOrDefault(e => e.Name.Equals("og:image", StringComparison.CurrentCultureIgnoreCase))?.Value;

            var date = DateTime.Now;
            return new EmbedBuilder()
                .WithAuthor(new EmbedAuthorBuilder()
                    .WithName(siteName + (channelName == null ? "" : $" - {channelName}"))
                    .WithIconUrl(WebHelper.GetIconUrlFromHtml(htmlCode))
                )
                .WithTitle(title)
                .WithThumbnailUrl(image)
                .WithDescription(description)
                .WithFooter(new EmbedFooterBuilder()
                    .WithText($"⏰ posted at {date:dddd, MMMM} {date.Day.Ordinal()} {date:yyyy HH:mm}")
                )
                .WithRssColour(guild)
                .WithUrl(url);
        }
    }
}
