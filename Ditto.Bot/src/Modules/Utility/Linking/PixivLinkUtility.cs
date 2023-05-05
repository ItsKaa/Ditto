using Discord;
using Discord.Commands;
using Ditto.Attributes;
using Ditto.Bot.Database.Data;
using Ditto.Data.Commands;
using Ditto.Data.Discord;
using Ditto.Extensions;
using Ditto.Helpers;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.IO;
using Discord.WebSocket;
using Ditto.Bot.Modules.Admin;
using Tweetinvi.Parameters;

namespace Ditto.Bot.Modules.Utility.Linking
{
    [Alias("pixiv")]
    public class PixivLinkUtility : DiscordModule<LinkUtility>
    {
        private static DateTime LastFetchTime { get; set; } = DateTime.MinValue;
        private static TimeSpan FetchTimeout { get; set; } = TimeSpan.FromMinutes(15);
        private static CookieContainer CookieContainer { get; set; } = new CookieContainer();
        private static IWebProxy Proxy { get; set; } = null;

        static PixivLinkUtility()
        {
            Ditto.Initialised += () =>
            {
                if (string.IsNullOrEmpty(Ditto.Settings.Credentials.PixivSessionId))
                {
                    Log.Warn("Pixiv session id is not configured, R18 content cannot be shown.");
                }
                else
                {
                    CookieContainer.Add(new Uri("https://imp.pixiv.net"), new Cookie("PHPSESSID", Ditto.Settings.Credentials.PixivSessionId));
                    CookieContainer.Add(new Uri("https://www.pixiv.net"), new Cookie("PHPSESSID", Ditto.Settings.Credentials.PixivSessionId));
                }

                if (Ditto.Settings.ProxySettings.Enabled)
                {
                    Proxy = new WebProxy(Ditto.Settings.ProxySettings.Host, Ditto.Settings.ProxySettings.Port);
                    if (!string.IsNullOrEmpty(Ditto.Settings.ProxySettings.Username))
                    {
                        Proxy.Credentials = new NetworkCredential(Ditto.Settings.ProxySettings.Username, Ditto.Settings.ProxySettings.Password);
                    }
                }

                return Task.CompletedTask;
            };

            LinkUtility.TryAddHandler(LinkType.Pixiv, async (link, channel, cancellationToken) =>
            {
                if ((DateTime.UtcNow - LastFetchTime) < FetchTimeout)
                {
                    return Enumerable.Empty<string>();
                }
                else if (Admin.Admin.CacheChannel == null)
                {
                    LastFetchTime = DateTime.UtcNow;
                    Log.Warn("Cache channel is not set, ignoring Pixiv module.");
                    return Enumerable.Empty<string>();
                }
                else if (!await Permissions.CanBotSendMessages(Admin.Admin.CacheChannel).ConfigureAwait(false))
                {
                    LastFetchTime = DateTime.UtcNow;
                    Log.Warn("No SendMessages permission in cache channel, ignoring Pixiv module.");
                    return Enumerable.Empty<string>();
                }

                // Already update fetch time
                LastFetchTime = DateTime.UtcNow;

                var processedIds = new List<string>();
                var userId = link.Value;
                var allIds = await GetPixivUserIllustrationIdList(userId).ConfigureAwait(false);
                var alreadyLinkedIds = link.Links.Select(x => x.Identity);
                var ids = allIds.Except(alreadyLinkedIds);

                if (ids.Count() > 0)
                {
                    // Wait a little since we just retrieved the ID list.
                    await Task.Delay(Randomizer.Static.New(5000, 10000));

                    var retryCount = 0;
                    for (int i = 0; i < ids.Count(); i++)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            return processedIds;
                        }

                        var illustrationId = ids.ElementAt(i);
                        try
                        {
                            Log.Info($"Fetching pixiv illustration id {illustrationId}...");
                            var htmlCode = await GetPixivIllustrationPageHtmlCode(userId, illustrationId).ConfigureAwait(false);
                            await Task.Delay(Randomizer.Static.New(5000, 10000));
                            if (string.IsNullOrEmpty(htmlCode))
                                continue;

                            try
                            {
                                var searchBlock = htmlCode.From($"\"illust\":{{\"{illustrationId}\"");
                                var title = searchBlock.Between(@"""illustTitle"":""", @""",""illustComment");
                                var authorName = searchBlock.Between(@"userName"":""", @"""}");

                                var createdDateString = searchBlock.Between(@"""uploadDate"":""", @""",""restrict");
                                var createdDate = DateTime.UtcNow;
                                if (!DateTime.TryParse(createdDateString, out createdDate))
                                {
                                    Log.Error("Failed to parse the date, assuming that the Pixiv illustration is posted now!");
                                }

                                // If the created date is older than the link date it most likely means that the link is new.
                                if (createdDate < link.Date)
                                {
                                    // We can assume that all the ids below this are old.
                                    processedIds.AddRange(ids.FromIndex(i, false));
                                    break;
                                }

                                var imagePath = searchBlock.Between(@"""regular"":""https://i.pximg.net", @""",""original");
                                var imageBytes = await GetPixivIllustrationImageBytes(imagePath).ConfigureAwait(false);
                                await Task.Delay(Randomizer.Static.New(5000, 10000));

                                Log.Info($"Posting \"{title}\" ({LinkType.Pixiv}) in \"{link.Guild.Name}:{link.Channel.Name}\"");
                                if (await PostPixivIllustration(link,
                                    illustrationId,
                                    title,
                                    authorName,
                                    createdDate,
                                    imageBytes
                                ).ConfigureAwait(false))
                                {
                                    processedIds.Add(illustrationId);
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Debug(ex);
                                continue;
                            }
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
                return processedIds;
            });
        }

        [DiscordCommand(CommandSourceLevel.Guild, CommandAccessLevel.Local)]
        [Alias("add", "link", "hook", "register")]
        public async Task Add(ITextChannel textChannel, [Multiword] string url, DateTime? fromDate = null)
        {
            if (!(await Ditto.Client.DoAsync(
                    c => c.GetPermissionsAsync(textChannel)
                ).ConfigureAwait(false)).HasAccess())
            {
                await Context.ApplyResultReaction(CommandResult.FailedBotPermission).ConfigureAwait(false);
            }
            else if (textChannel != null && textChannel.Guild != Context.Guild)
            {
                await Context.ApplyResultReaction(CommandResult.FailedUserPermission).ConfigureAwait(false);
            }
            else if (!WebHelper.IsValidWebsite(url))
            {
                await Context.ApplyResultReaction(CommandResult.Failed).ConfigureAwait(false);
            }
            else
            {
                var match = Globals.RegularExpression.PixivUserIdFromUrl.Match(url);
                if (!match.Success
                    || !match.Groups.ContainsKey("id")
                    || !int.TryParse(match.Groups["id"].Value, out int userId)
                    || userId <= 0)
                {
                    await Context.ApplyResultReaction(CommandResult.Failed).ConfigureAwait(false);
                    return;
                }
                
                // Check for an existing item
                var uri = WebHelper.ToUri(url);
                if (await LinkUtility.TryAddLinkAsync(LinkType.Pixiv, textChannel, $"{userId}", fromDate) != null)
                {
                    await Context.ApplyResultReaction(CommandResult.Success).ConfigureAwait(false);
                }
                else
                {
                    await Context.ApplyResultReaction(CommandResult.Failed).ConfigureAwait(false);
                    await Context.EmbedAsync(
                        $"That page is already linked to {textChannel.Mention}",
                        ContextMessageOption.ReplyWithError
                    ).ConfigureAwait(false);
                    return;
                }

            }
        }

        protected static async Task<bool> PostPixivIllustration(Database.Models.Link link, string illustrationId, string title, string authorName, DateTime dateTime, byte[] imageBytes)
        {
            if(Admin.Admin.CacheChannel == null || !await Permissions.CanBotSendMessages(Admin.Admin.CacheChannel).ConfigureAwait(false))
            {
                Log.Error("Cache channel is not set or permissions have changed!");
                return false;
            }

            var imageUrl = "";
            try
            {
                using var memoryStream = new MemoryStream(imageBytes);
                var fileMessage = await Admin.Admin.CacheChannel.SendFileAsync(
                    memoryStream,
                    $"{illustrationId}.png",
                    options: new RequestOptions()
                    {
                        RetryMode = RetryMode.AlwaysRetry
                    }
                ).ConfigureAwait(false);
                imageUrl = fileMessage.Attachments.FirstOrDefault()?.Url ?? string.Empty;
            }
            catch (Exception ex)
            {
                Log.Error("Failed to upload attachment to cache channel", ex);
                return false;
            }

            var date = dateTime.ToUniversalTime();
            var embed = new EmbedBuilder()
                .WithAuthor(new EmbedAuthorBuilder()
                    .WithName($"[Pixiv] {authorName}")
                    .WithIconUrl("https://s.pximg.net/common/images/apple-touch-icon.png")
                )
                .WithTitle(title)
                .WithFooter(new EmbedFooterBuilder()
                    .WithText($"⏰ {date:dddd, MMMM} {date.Day.Ordinal()} {date:yyyy HH:mm} UTC")
                )
                .WithImageUrl(imageUrl)
                .WithColor(1, 151, 250) // pixiv blue
                .WithUrl($"https://www.pixiv.net/en/artworks/{illustrationId}");

            await link.Channel.EmbedAsync(embed,
                options: new RequestOptions() { RetryMode = RetryMode.AlwaysRetry}
            ).ConfigureAwait(false);
            return true;
        }


        private static async Task<string> GetPixivUserIllustrations(string userId)
        {
            try
            {
                using var handler = new HttpClientHandler()
                {
                    CookieContainer = CookieContainer,
                    UseCookies = true,
                    Proxy = Proxy,
                    UseProxy = true,
                };

                using var client = new HttpClient(handler)
                {
                    BaseAddress = new Uri("https://www.pixiv.net")
                };

                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/104.0.0.0 Safari/537.36");
                client.DefaultRequestHeaders.Host = "www.pixiv.net";
                client.DefaultRequestHeaders.Referrer = new Uri($"https://www.pixiv.net/en/users/{userId}/illustrations");
                client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
                client.DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip");
                client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.5");
                client.DefaultRequestHeaders.Connection.ParseAdd("keep-alive");
                client.DefaultRequestHeaders.Add("DNT", "1");
                client.DefaultRequestHeaders.Add("Alt-Used", "www.pixiv.net");
                client.DefaultRequestHeaders.TE.ParseAdd("trailers");
                client.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "empty");
                client.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "cors");
                client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-origin");

                var message = new HttpRequestMessage(HttpMethod.Get, $"/ajax/user/{userId}/profile/all");
                var result = await client.SendAsync(message);
                result.EnsureSuccessStatusCode();
                return await WebHelper.ReadContentAsString(result).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Debug(ex);
            }

            return null;
        }

        private static async Task<string> GetPixivIllustrationPageHtmlCode(string userId, string illustrationId)
        {
            try
            {
                using var handler = new HttpClientHandler()
                {
                    CookieContainer = CookieContainer,
                    UseCookies = true,
                    Proxy = Proxy,
                    UseProxy = true,
                };

                using var client = new HttpClient(handler)
                {
                    BaseAddress = new Uri("https://www.pixiv.net")
                };

                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/104.0.0.0 Safari/537.36");
                client.DefaultRequestHeaders.Host = "www.pixiv.net";
                client.DefaultRequestHeaders.Referrer = new Uri($"https://www.pixiv.net/en/users/{userId}/illustrations");
                client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
                client.DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip, deflate, br");
                client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
                client.DefaultRequestHeaders.Connection.ParseAdd("keep-alive");
                client.DefaultRequestHeaders.Add("max-age", "0");
                client.DefaultRequestHeaders.Add("DNT", "1");
                client.DefaultRequestHeaders.Add("Alt-Used", "www.pixiv.net");
                client.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "empty");
                client.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "cors");
                client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-origin");

                var message = new HttpRequestMessage(HttpMethod.Get, $"/en/artworks/{illustrationId}");
                var result = await client.SendAsync(message);
                result.EnsureSuccessStatusCode();
                return await WebHelper.ReadContentAsString(result);
            }
            catch (Exception ex)
            {
                Log.Debug(ex);
            }

            return null;
        }

        private static async Task<byte[]> GetPixivIllustrationImageBytes(string imagePath)
        {
            try
            {
                using var handler = new HttpClientHandler()
                {
                    CookieContainer = CookieContainer,
                    UseCookies = true,
                    Proxy = Proxy,
                    UseProxy = true,
                };

                using var client = new HttpClient(handler)
                {
                    BaseAddress = new Uri("https://i.pximg.net")
                };

                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/104.0.0.0 Safari/537.36");
                client.DefaultRequestHeaders.Host = "i.pximg.net";
                client.DefaultRequestHeaders.Referrer = new Uri("https://www.pixiv.net/");
                client.DefaultRequestHeaders.Accept.ParseAdd("image/avif,image/webp,*/*");
                client.DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip, deflate, br");
                client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.5");
                client.DefaultRequestHeaders.Connection.ParseAdd("keep-alive");
                client.DefaultRequestHeaders.Add("DNT", "1");
                client.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "image");
                client.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "no-cors");
                client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "cross-site");

                var message = new HttpRequestMessage(HttpMethod.Get, imagePath);
                var result = await client.SendAsync(message);
                result.EnsureSuccessStatusCode();
                return await result.Content.ReadAsByteArrayAsync();
            }
            catch (Exception ex)
            {
                Log.Debug(ex);
            }

            return Array.Empty<byte>();
        }

        private static async Task<IEnumerable<string>> GetPixivUserIllustrationIdList(string userId)
        {
            var illustrationIds = new List<string>();
            try
            {
                var jsonString = await GetPixivUserIllustrations(userId);
                if (!string.IsNullOrEmpty(jsonString))
                {
                    var jsonObjects = JObject.Parse(jsonString)?.Children().OfType<JProperty>();
                    if (jsonObjects?.Count() > 0)
                    {
                        var body = jsonObjects.SingleOrDefault(e => e.Name == "body")?.Value;
                        var bodyChildren = body?.Children().OfType<JProperty>();
                        if (bodyChildren?.Count() > 0)
                        {
                            var illustrations = bodyChildren?.SingleOrDefault(e => e.Name == "illusts")?.Value.AsJEnumerable();
                            foreach (var illustrationId in illustrations!.OfType<JProperty>().Select(x => x.Name))
                            {
                                illustrationIds.Add(illustrationId);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex);
            }

            return illustrationIds;
        }
    }
}