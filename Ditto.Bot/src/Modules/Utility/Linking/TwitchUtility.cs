using Discord;
using Discord.Commands;
using Ditto.Attributes;
using Ditto.Bot.Database.Data;
using Ditto.Bot.Database.Models;
using Ditto.Data.Commands;
using Ditto.Data.Discord;
using Ditto.Extensions;
using Ditto.Helpers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
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
                        Monitor = new LiveStreamMonitorService(Ditto.Twitch, 60);
                        Monitor.OnStreamOnline += Monitor_OnStreamOnline;
                        Monitor.OnStreamOffline += Monitor_OnStreamOffline;

                        // Start monitoring the twitch links, this will notify users when a stream switches it's live status.
                        var channels = Links.ToList().Select(e => e.Value.Value).ToList();
                        MonitorChannels(channels);

                        // Instantly update the monitoring service on load.
                        if (Links.Count > 0)
                        {
                            await Monitor.UpdateLiveStreamersAsync(true).ConfigureAwait(false);
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
                Monitor?.Stop();
                Monitor = null;
                return Task.CompletedTask;
            };
        }

        private static void MonitorChannels(IEnumerable<string> channelNames)
        {
            try
            {
                if (IsMonitoring)
                {
                    Monitor.Stop();
                }

                var channels = Links.ToList().Select(e => e.Value.Value).ToList();
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
            MonitorChannels(links.Select(e => e.Value));
        }

        private static void Monitor_OnStreamOffline(object sender, OnStreamOfflineArgs args)
        {
            // TODO: What to do with offline streams? edit the original message? delete? a reaction?
            // Probably good to add a config option in the DB.

            //var links = Links.Values.Where(e => string.Equals(e.Value, args.Channel, StringComparison.CurrentCultureIgnoreCase));
            //foreach(var link in links)
            //{
            //    await link.Channel.SendMessageAsync($"[Twitch] Stream '{args.Channel}' went offline.").ConfigureAwait(false);
            //}
        }
        private static async void Monitor_OnStreamOnline(object sender, OnStreamOnlineArgs args)
        {
            // TODO: Use the 'Date' field in the link database to determine whether we should post it.

            var links = Links.Values.Where(e => string.Equals(e.Value, args.Channel, StringComparison.CurrentCultureIgnoreCase));
            foreach(var link in links)
            {
                // Attempt to retrieve the V5 stream data for more detailed info.
                TwitchLib.Api.V5.Models.Streams.Stream stream = null;
                try
                {
                    stream = (await Ditto.Twitch.V5.Streams.GetStreamByUserAsync(args.Stream.UserId, args.Stream.Type).ConfigureAwait(false))?.Stream;
                }
                catch { }

                var streamImageUrl = args.Stream.ThumbnailUrl.Replace("{width}", "800").Replace("{height}", "600");
                var embedBuilder = new EmbedBuilder()
                    //.WithTitle($"[Twitch] {args.Channel}")
                    //.WithDescription(args.Stream.Title)
                    .WithTitle(args.Stream.Title)
                    .WithFields(
                        //new EmbedFieldBuilder().WithName("Description").WithValue(args.Stream.Title).WithIsInline(false),
                        new EmbedFieldBuilder().WithName("Playing").WithValue(stream?.Game ?? $"Unknown ({args.Stream.GameId})").WithIsInline(true),
                        new EmbedFieldBuilder().WithName("Viewers").WithValue(args.Stream.ViewerCount).WithIsInline(true)
                    )
                    .WithFooter(efb => efb.WithText($"started at {args.Stream.StartedAt:dd-MM-yyyy HH:mm:ss}"))
                    .WithUrl($"https://twitch.tv/{args.Channel}")
                    .WithThumbnailUrl(streamImageUrl)
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithIconUrl(stream.Channel.Logo)
                        .WithName($"[Twitch] {args.Channel}")
                        .WithUrl($"https://twitch.tv/{args.Channel}")
                    )
                    .WithOkColour(link.Guild)
                ;

                if (link.Channel != null)
                {
                    await link.Channel.EmbedAsync(embedBuilder,
                        options: new RequestOptions() { RetryMode = RetryMode.RetryRatelimit | RetryMode.RetryTimeouts }
                    ).ConfigureAwait(false);
                }
                else
                {
                    // TODO: We should make certain that we this channel exists by invoking Ditto.Client.DoAsync(...) and then delete it if necessary.
                    Log.Debug($"Twitch | Link #{link.Id} doesn't have a channel, clean-up database?");
                }
            }
        }

        [DiscordCommand(CommandSourceLevel.Guild, CommandAccessLevel.LocalAndParents)]
        public Task Twitch([Optional] ITextChannel textChannel, [Multiword] string channelOrUrl)
            => Add(textChannel, channelOrUrl);

        [DiscordCommand(CommandSourceLevel.Guild, CommandAccessLevel.Local)]
        [Alias("add", "link", "hook", "register")]
        public async Task Add([Optional] ITextChannel textChannel, [Multiword] string channelOrUrl)
        {
            if(textChannel == null)
                textChannel = Context.TextChannel;

            if (!(await Ditto.Client.DoAsync(c => c.GetPermissionsAsync(textChannel)).ConfigureAwait(false)).HasAccess())
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
                    var link = await LinkUtility.TryAddLinkAsync(LinkType.Twitch, textChannel, channelName, (left, right) =>
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

    }
}
