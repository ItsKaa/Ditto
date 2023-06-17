using Discord;
using Discord.Commands;
using Ditto.Attributes;
using Ditto.Bot.Database.Data;
using Ditto.Bot.Database.Models;
using Ditto.Bot.Modules.Admin;
using Ditto.Data.Commands;
using Ditto.Data.Discord;
using Ditto.Extensions;
using Ditto.Helpers;
using SixLabors.ImageSharp.Formats;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using TwitchLib.Api.Helix;
using TwitchLib.Api.Services;
using TwitchLib.Api.Services.Events.LiveStreamMonitor;

namespace Ditto.Bot.Modules.Utility.Linking
{
    [Alias("twitch")]
    public class TwitchUtility : DiscordModule<LinkUtility>
    {
        private static CancellationTokenSource _cancellationTokenSource;
        private static ConcurrentDictionary<int, Link> Links;
        private static LiveStreamMonitorService Monitor { get; set; }
        public static bool IsMonitoring { get; private set; }

        static TwitchUtility()
        {
            if (Ditto.Twitch == null)
                return;

            Links = new ConcurrentDictionary<int, Link>();
            IsMonitoring = false;

            Ditto.Connected += () =>
            {
                var _ = Task.Run(async () =>
                {
                    _cancellationTokenSource?.Cancel();
                    _cancellationTokenSource = new CancellationTokenSource();

                    await Ditto.Database.ReadAsync((uow) =>
                    {
                        Links = new ConcurrentDictionary<int, Link>(
                            uow.Links.GetAllWithLinks()
                            .Where(e => e.Type == LinkType.Twitch)
                            .Select(i => new KeyValuePair<int, Link>(i.Id, i))
                        );
                    });

                    try
                    {
                        if (Monitor == null)
                        {
                            Monitor = new LiveStreamMonitorService(Ditto.Twitch, 60);
                            Monitor.OnStreamOnline += Monitor_OnStreamOnline;
                            Monitor.OnStreamOffline += Monitor_OnStreamOffline;
                            Monitor.OnStreamUpdate += Monitor_OnStreamUpdate;

                            // Start monitoring the twitch links, this will notify users when a stream switches it's live status.
                            var channels = Links.ToList().Select(e => e.Value.Value.Split("|", StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()).ToList();
                            MonitorChannels(channels);

                            // Instantly update the monitoring service on load.
                            if (Links.Count > 0)
                            {
                                await Monitor.UpdateLiveStreamersAsync(true).ConfigureAwait(false);
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        Log.Error($"Twitch | {ex}");
                    }

                });
                return Task.CompletedTask;
            };

            Ditto.Exit += () =>
            {
                _cancellationTokenSource?.Cancel();
                Links?.Clear();
                return Task.CompletedTask;
            };
        }

        private static void Monitor_OnStreamUpdate(object sender, OnStreamUpdateArgs e)
        {
            Monitor_OnStreamOnline(sender, new OnStreamOnlineArgs()
            {
                Channel = e.Channel,
                Stream = e.Stream
            });
        }

        private static void MonitorChannels(IEnumerable<string> channelNames)
        {
            try
            {
                if (IsMonitoring)
                {
                    Monitor.Stop();
                }

                var channels = Links.ToList().Select(e => e.Value.Value.Split("|", StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()).ToList();
                channels.AddRange(channelNames);
                Monitor.SetChannelsByName(channels.Distinct().ToList());
                Monitor.Start();
                IsMonitoring = true;
            }
            catch { }
        }
        private static void MonitorLink(Link link)
        {
            MonitorLink(new[] { link });
        }
        private static void MonitorLink(IEnumerable<Link> links)
        {
            MonitorChannels(links.Select(e => e.Value.Split("|", StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()));
        }

        private static void Monitor_OnStreamOffline(object sender, OnStreamOfflineArgs args)
        {
            Task.Run(async () =>
            {
                try
                {
                    var links = Links.Values.Where(e => string.Equals(e.Value.Split("|", StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(), args.Channel, StringComparison.CurrentCultureIgnoreCase));
                    foreach (var link in links)
                    {
                        var linkValues = link.Value.Split("|", StringSplitOptions.RemoveEmptyEntries);
                        var streamName = linkValues.FirstOrDefault();
                        ulong discordMessageId = 0;
                        if (linkValues.Length > 4)
                        {
                            ulong.TryParse(linkValues[4], out discordMessageId);
                        }

                        Log.Debug($"Twitch | {args?.Stream?.UserName} went offline (Id: {args?.Stream?.Id}).");

                        try
                        {
                            // Update discord message, if it still exists.
                            var discordMessage = (await link.Channel.GetMessageAsync(discordMessageId,
                                CacheMode.AllowDownload,
                                new RequestOptions() { RetryMode = RetryMode.AlwaysRetry }
                            ).ConfigureAwait(false)) as IUserMessage;

                            if (discordMessage != null)
                            {
                                var embed = discordMessage.Embeds.FirstOrDefault();
                                var embedBuilder = new EmbedBuilder()
                                    .WithTitle(embed.Title)
                                    .WithAuthor(embed.Author?.Name, embed.Author?.IconUrl, embed.Author?.Url)
                                    .WithDescription(embed.Description)
                                    .WithFooter(embed.Footer?.Text, embed.Footer?.IconUrl)
                                    .WithUrl(embed.Url)
                                    .WithThumbnailUrl(embed.Thumbnail?.Url)
                                    .WithFields(embed.Fields.Select(x =>
                                        new EmbedFieldBuilder()
                                        .WithIsInline(x.Inline)
                                        .WithName(x.Name)
                                        .WithValue(x.Value))
                                    )
                                    .WithColor(Color.Red)
                                    ;
                                await discordMessage.ModifyAsync(x => x.Embed = embedBuilder.Build(), new RequestOptions() { RetryMode = RetryMode.AlwaysRetry }).ConfigureAwait(false);
                            }
                        }
                        catch { }

                        // Update link
                        link.Value = $"{streamName}|{long.MinValue}|{DateTime.MinValue.Ticks}|{DateTime.MinValue.Ticks}|{ulong.MinValue}";
                        await Ditto.Database.DoAsync((uow) =>
                        {
                            uow.Links.Update(link);
                        }).ConfigureAwait(false);
                    }
                }
                catch(Exception ex)
                {
                    Log.Error($"Error while updating to offline: {ex}");
                }
            });
        }
            private static void Monitor_OnStreamOnline(object sender, OnStreamOnlineArgs args)
        {
            Task.Run(async () =>
            {
                // TODO: Use the 'Date' field in the link database to determine whether we should post it.

                var links = Links.Values.Where(e => string.Equals(e.Value.Split("|", StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(), args.Channel, StringComparison.CurrentCultureIgnoreCase));
                foreach (var link in links)
                {
                    try
                    {
                        var linkValues = link.Value.Split("|", StringSplitOptions.RemoveEmptyEntries);
                        var streamName = linkValues.FirstOrDefault();
                        long linkStreamId = -1;
                        var linkDateCreated = DateTime.MinValue;
                        var linkDateUpdated = DateTime.MinValue;
                        ulong discordMessageId = 0;
                        if (linkValues.Length > 1)
                        {
                            long.TryParse(linkValues[1], out linkStreamId);
                        }
                        if (linkValues.Length > 2)
                        {
                            if (long.TryParse(linkValues[2], out long ticks))
                            {
                                linkDateCreated = new DateTime(ticks, DateTimeKind.Utc);
                            }
                        }
                        if (linkValues.Length > 3)
                        {
                            if (long.TryParse(linkValues[3], out long ticks))
                            {
                                linkDateUpdated = new DateTime(ticks, DateTimeKind.Utc);
                            }
                        }
                        if (linkValues.Length > 4)
                        {
                            ulong.TryParse(linkValues[4], out discordMessageId);
                        }

                        // Attempt to retrieve the V5 stream data for more detailed info.
                        TwitchLib.Api.V5.Models.Streams.Stream stream = null;
                        try
                        {
                            stream = (await Ditto.Twitch.V5.Streams.GetStreamByUserAsync(args.Stream.UserId, args.Stream.Type).ConfigureAwait(false))?.Stream;
                        }
                        catch { }

                        if (stream != null)
                        {
                            if (discordMessageId == 0 || (stream.Id != linkStreamId && stream.CreatedAt > linkDateCreated))
                            {
                                linkStreamId = stream.Id;
                                linkDateCreated = stream.CreatedAt;
                                linkDateUpdated = DateTime.UtcNow;

                                Log.Debug($"Twitch | {stream?.Channel?.Name} went live (Id: {stream?.Id} at {stream?.CreatedAt.ToLongTimeString()}).");

                                var embedBuilder = GetTwitchEmbedMessage(args.Stream, stream, link);

                                if (link.Channel != null)
                                {
                                    var message = await link.Channel.EmbedAsync(embedBuilder,
                                        options: new RequestOptions() { RetryMode = RetryMode.RetryRatelimit | RetryMode.RetryTimeouts }
                                    ).ConfigureAwait(false);
                                    discordMessageId = message?.Id ?? 0;
                                }
                                else
                                {
                                    Log.Debug($"Twitch | Link #{link.Id} doesn't have a channel, clean-up database?");
                                }
                            }
                            else
                            {
                                var discordMessage = (await link.Channel.GetMessageAsync(discordMessageId,
                                    CacheMode.AllowDownload,
                                    new RequestOptions() { RetryMode = RetryMode.AlwaysRetry }
                                ).ConfigureAwait(false)) as IUserMessage;

                                if (discordMessage?.Author?.IsBot == true)
                                {
                                    var embedBuilder = GetTwitchEmbedMessage(args.Stream, stream, link);
                                    await discordMessage.ModifyAsync(x => x.Embed = embedBuilder.Build(), new RequestOptions() { RetryMode = RetryMode.AlwaysRetry }).ConfigureAwait(false);
                                    discordMessageId = discordMessage.Id;
                                    linkDateCreated = stream.CreatedAt;
                                    linkDateUpdated = DateTime.UtcNow;
                                }
                                else
                                {
                                    discordMessageId = 0;
                                    linkDateCreated = DateTime.MinValue;
                                    linkDateUpdated = DateTime.MinValue;
                                }
                            }

                            // Update link
                            link.Value = $"{streamName}|{stream.Id}|{linkDateCreated.ToUniversalTime().Ticks}|{linkDateUpdated.Ticks}|{discordMessageId}";
                            await Ditto.Database.DoAsync((uow) =>
                            {
                                uow.Links.Update(link);
                            }).ConfigureAwait(false);
                        }
                    }
                    catch { }
                }
            });
        }

        private static EmbedBuilder GetTwitchEmbedMessage(TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream stream, TwitchLib.Api.V5.Models.Streams.Stream streamV5, Link link)
        {
            var channelName = (streamV5?.Channel?.DisplayName ?? streamV5?.Channel?.Name ?? stream?.UserName ?? "Unknown");
            var streamImageUrl = stream.ThumbnailUrl.Replace("{width}", "800").Replace("{height}", "600");
            var game = streamV5?.Game;
            if (string.IsNullOrEmpty(game))
            {
                game = $"Unknown ({stream?.GameId}";
            }

            var embedBuilder = new EmbedBuilder()
                //.WithTitle($"[Twitch] {args.Channel}")
                //.WithDescription(args.Stream.Title)
                .WithTitle(stream.Title)
                .WithFields(
                    //new EmbedFieldBuilder().WithName("Description").WithValue(args.Stream.Title).WithIsInline(false),
                    new EmbedFieldBuilder().WithName("Playing").WithValue(game).WithIsInline(true),
                    new EmbedFieldBuilder().WithName("Viewers").WithValue(stream.ViewerCount).WithIsInline(true)
                )
                .WithFooter(efb => efb.WithText($"started at {stream.StartedAt:dd-MM-yyyy HH:mm:ss}"))
                .WithUrl($"https://twitch.tv/{channelName}")
                .WithThumbnailUrl(streamImageUrl)
                .WithAuthor(new EmbedAuthorBuilder()
                    .WithIconUrl(streamV5?.Channel?.Logo ?? "")
                    .WithName($"[Twitch] {channelName}")
                    .WithUrl($"https://twitch.tv/{channelName}")
                )
                .WithTwitchColour(link.Guild)
            ;
            

            return embedBuilder;
        }

        [DiscordCommand(CommandSourceLevel.Guild, CommandAccessLevel.Local)]
        [Alias("add", "link", "hook", "register")]
        public async Task Add(ITextChannel textChannel, [Multiword] string channelOrUrl)
        {
            if(!Permissions.IsAdministratorOrBotOwner(Context))
            {
                await Context.ApplyResultReaction(CommandResult.FailedUserPermission).ConfigureAwait(false);
                return;
            }

            if (textChannel == null)
                textChannel = Context.TextChannel;

            // Only allow using channels of the current guild.
            if (textChannel != null && textChannel.Guild != Context.Guild)
            {
                await Context.ApplyResultReaction(CommandResult.FailedUserPermission).ConfigureAwait(false);
                return;
            }

            if (!(await Ditto.Client.GetPermissionsAsync(textChannel)).HasAccess())
            {
                await Context.ApplyResultReaction(CommandResult.FailedBotPermission).ConfigureAwait(false);
                return;
            }
            else
            {
                if(Ditto.Twitch == null)
                {
                    await Context.ApplyResultReaction(CommandResult.Failed).ConfigureAwait(false);
                    return;
                }
                else if (WebHelper.IsValidWebsite(channelOrUrl) && WebHelper.ToUri(channelOrUrl)?.Host != "twitch.tv")
                {
                    await Context.ApplyResultReaction(CommandResult.Failed).ConfigureAwait(false);
                    return;
                }
                else
                {
                    var channelName = Path.GetFileName(channelOrUrl).Trim();
                    var link = await LinkUtility.TryAddLinkAsync(LinkType.Twitch, textChannel, $"{channelName}|{long.MinValue}|{DateTime.MinValue.Ticks}|{DateTime.MinValue.Ticks}|{ulong.MinValue}", (left, right) =>
                    {
                        //return WebHelper.Compare(WebHelper.ToUri(left), WebHelper.ToUri(right));
                        return string.Equals(left, right, StringComparison.CurrentCultureIgnoreCase);
                    });
                    if (link == null)
                    {
                        await Context.EmbedAsync(
                            $"That url is already linked to {textChannel?.Mention}",
                            ContextMessageOption.ReplyWithError
                        ).ConfigureAwait(false);
                        await Context.ApplyResultReaction(CommandResult.Failed).ConfigureAwait(false);
                        return;
                    }
                    else
                    {
                        if(Links.TryAdd(link.Id, link))
                        {
                            MonitorLink(link);
                        }
                        await Context.ApplyResultReaction(CommandResult.Success).ConfigureAwait(false);
                    }
                }
            }
        }

        [DiscordCommand(CommandSourceLevel.Guild, CommandAccessLevel.Local)]
        [Alias("add", "link", "hook", "register")]
        public Task Add([Multiword] string channelOrUrl, ITextChannel textChannel)
            => Add(textChannel, channelOrUrl);

        [DiscordCommand(CommandSourceLevel.Guild, CommandAccessLevel.Global)]
        public Task Twitch(ITextChannel textChannel, [Multiword] string channelOrUrl)
            => Add(textChannel, channelOrUrl);

        [DiscordCommand(CommandSourceLevel.Guild, CommandAccessLevel.Global)]
        public Task Twitch([Multiword] string channelOrUrl, ITextChannel textChannel = null)
            => Add(textChannel, channelOrUrl);


    }
}
