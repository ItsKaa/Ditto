using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Ditto.Attributes;
using Ditto.Bot.Database.Data;
using Ditto.Bot.Modules.Admin;
using Ditto.Bot.Services;
using Ditto.Data.Commands;
using Ditto.Data.Discord;
using Ditto.Extensions;
using Ditto.Helpers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Ditto.Bot.Modules.Utility.Linking
{
    [Alias("discord")]
    public class DiscordLinkUtility : DiscordTextModule<LinkUtility>
    {
        private static ConcurrentDictionary<int, DateTime> LastUpdate { get; set; }

        static DiscordLinkUtility()
        {
            LastUpdate = new ConcurrentDictionary<int, DateTime>();

            LinkUtility.TryAddHandler(LinkType.Discord, async (link, channel, cancellationToken) =>
            {
                var messageIds = new List<string>();

                // Only pull discord channel feeds every 2 minutes for each individual channel.
                var lastUpdateTime = LastUpdate.GetOrAdd(link.Id, DateTime.MinValue);
                if ((DateTime.UtcNow - lastUpdateTime).TotalSeconds < 120)
                {
                    return messageIds;
                }

                if (link != null
                    && ulong.TryParse(link.Value, out ulong linkChannelId)
                    && (await Ditto.Client.GetChannelAsync(linkChannelId)) is ITextChannel linkChannel)
                {
                    // Retrieve the latest messages in bulk from the targeted channel.
                    var messages = new List<IMessage>();
                    ulong lastMessageId = ulong.MaxValue;
                    while (true)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            return messageIds;
                        }

                        var messagesChunk = Enumerable.Empty<IMessage>();
                        if (lastMessageId != ulong.MaxValue)
                        {
                            messagesChunk = (await linkChannel.GetMessagesAsync(lastMessageId, Direction.Before, 100, CacheMode.AllowDownload).ToListAsync().ConfigureAwait(false))
                                .SelectMany(m => m)
                                .Where(m => m.CreatedAt.UtcDateTime > link.Date)
                                .Where(m => null == link.Links.FirstOrDefault(l => l.Identity == m.Id.ToString()));
                        }
                        else
                        {
                            messagesChunk = (await linkChannel.GetMessagesAsync(100, CacheMode.AllowDownload).ToListAsync().ConfigureAwait(false))
                                .SelectMany(m => m)
                                .Where(m => m.CreatedAt.UtcDateTime > link.Date)
                                .Where(m => null == link.Links.FirstOrDefault(l => l.Identity == m.Id.ToString()));
                        }
                        messages.AddRange(messagesChunk);
                        lastMessageId = messagesChunk.LastOrDefault()?.Id ?? ulong.MaxValue;

                        // Cancel when message count is less than the maximum.
                        if (messagesChunk.Count() < 100)
                        {
                            break;
                        }
                    }

                    // Update link date-time.
                    var lastMessageDate = DateTime.MinValue;
                    var funcUpdateLinkDate = new Func<Task>(async () =>
                    {
                        if (lastMessageDate > link.Date)
                        {
                            link.Date = lastMessageDate;
                            await Ditto.Database.WriteAsync(uow =>
                            {
                                uow.Links.Update(link);
                            }).ConfigureAwait(false);

                            await Task.Delay(10).ConfigureAwait(false);
                        }
                    });


                    // Attempt to post the messages in sync with the created date.
                    try
                    {
                        var guildUsers = new List<IGuildUser>();
                        foreach (var message in messages.OrderBy(m => m.CreatedAt))
                        {
                            int retryCount = 0;
                            while (retryCount < 10)
                            {
                                // Cancel out where needed
                                if (cancellationToken.IsCancellationRequested)
                                {
                                    await funcUpdateLinkDate().ConfigureAwait(false);
                                    return messageIds;
                                }

                                // Attempt to send the message.
                                try
                                {
                                    var authorGuildUser = guildUsers.FirstOrDefault(x => x.Id == message.Author.Id);
                                    if (authorGuildUser == null)
                                    {
                                        try
                                        {
                                            authorGuildUser = await linkChannel.Guild.GetUserAsync(message.Author.Id).ConfigureAwait(false);
                                            if (authorGuildUser != null)
                                            {
                                                guildUsers.Add(authorGuildUser);
                                            }
                                        }
                                        catch { }
                                    }

                                    var messageContent = message.Content;

                                    // Parse discord emojis.
                                    string imageUrl = null;
                                    if (messageContent.ParseDiscordEmojis().FirstOrDefault() is DiscordTagResult parseResult)
                                    {
                                        if (parseResult.Type == DiscordTagType.EMOJI_ANIMATED)
                                        {
                                            imageUrl = $"https://cdn.discordapp.com/emojis/{parseResult.Id}.gif?size=48";
                                        }
                                        else if (parseResult.Type == DiscordTagType.EMOJI)
                                        {
                                            imageUrl = $"https://cdn.discordapp.com/emojis/{parseResult.Id}.webp?size=48";
                                        }
                                    }

                                    // Handle Tenor URLs
                                    var tenorMatch = Globals.RegularExpression.TenorGif.Match(messageContent);
                                    if (tenorMatch.Success)
                                    {
                                        var tenorUrl = tenorMatch.Value;
                                        messageContent = messageContent.Replace(tenorUrl, "");
                                        var tenorGifUrl = await WebHelper.GetResponseUrlAsync($"{tenorUrl}.gif").ConfigureAwait(false);
                                        if (!string.IsNullOrEmpty(tenorGifUrl))
                                        {
                                            imageUrl = tenorGifUrl;
                                        }
                                    }

                                    // Handle sticker
                                    if (message.Stickers.Any()
                                        && message.Stickers.FirstOrDefault() is SocketSticker sticker)
                                    {
                                        imageUrl = sticker.GetStickerUrl();
                                    }

                                    var dateUtc = message.CreatedAt.UtcDateTime;
                                    var embedBuilder = new EmbedBuilder().WithAuthor(new EmbedAuthorBuilder()
                                          .WithIconUrl(message.Author.GetAvatarUrl())
                                          .WithName(authorGuildUser?.Nickname ?? message.Author?.Username)
                                        )
                                        //.WithTitle(message.Channel.Name)
                                        .WithDescription(messageContent)
                                        .WithFooter($"{dateUtc:dddd, MMMM} {dateUtc.Day.Ordinal()} {dateUtc:yyyy} at {dateUtc:HH:mm} UTC")
                                        .WithDiscordLinkColour(channel.Guild)
                                        ;

                                    if (!string.IsNullOrEmpty(imageUrl))
                                    {
                                        embedBuilder = embedBuilder.WithImageUrl(imageUrl);
                                    }

                                    // Download the attachments if they are included so we can forward them in the same message.
                                    var fileStreams = new List<Stream>();
                                    var files = new List<FileAttachment>();
                                    foreach (var attachment in message.Attachments)
                                    {
                                        var attachmentUrl = attachment?.Url;
                                        if (!string.IsNullOrEmpty(attachmentUrl))
                                        {
                                            try
                                            {
                                                var stream = await WebHelper.GetStreamAsync(attachmentUrl).ConfigureAwait(false);
                                                files.Add(new FileAttachment(stream, attachment.Filename));
                                                fileStreams.Add(stream);
                                            }
                                            catch { }
                                        }
                                    }

                                    // Send the translated message.
                                    IUserMessage postedMessage = null;
                                    if (files.Count > 0)
                                    {
                                        postedMessage = await channel.SendFilesAsync(
                                            attachments: files,
                                            embed: embedBuilder.Build(),
                                            options: new RequestOptions() { RetryMode = RetryMode.AlwaysFail }
                                        ).ConfigureAwait(false);
                                    }
                                    else
                                    {
                                        postedMessage = await channel.SendMessageAsync(
                                            embed: embedBuilder.Build(),
                                            options: new RequestOptions() { RetryMode = RetryMode.AlwaysFail }
                                        ).ConfigureAwait(false);
                                    }

                                    if (postedMessage != null)
                                    {
                                        // Do not add the message to the messageIds, we do not use the link_items database for this.
                                        //messageIds.Add(message.Id.ToString());
                                        lastMessageDate = message.CreatedAt.UtcDateTime;
                                    }

                                    // OK, cancel out.
                                    break;
                                }
                                catch (Exception ex)
                                {
                                    // Update the link date just in case.
                                    await funcUpdateLinkDate().ConfigureAwait(false);

                                    // Cancel out where needed
                                    if (cancellationToken.IsCancellationRequested)
                                    {
                                        return messageIds;
                                    }

                                    // Attempt to retry sending the message
                                    if (!await LinkUtility.SendRetryLinkMessageAsync(link.Type, retryCount++, ex is Discord.Net.RateLimitedException ? null : ex))
                                    {
                                        return messageIds;
                                    }
                                }
                            }
                        }
                    }
                    finally
                    {
                        // Update the link date time.
                        await funcUpdateLinkDate().ConfigureAwait(false);
                    }
                }

                LastUpdate.TryUpdate(link.Id, DateTime.UtcNow, lastUpdateTime);
                return messageIds;
            });
        }

        public DiscordLinkUtility(DatabaseCacheService cache, DatabaseService database) : base(cache, database)
        {
        }

        [DiscordCommand(CommandSourceLevel.Guild, CommandAccessLevel.Local)]
        [Alias("add", "link", "hook", "register")]
        public async Task Add(
            [Help("sourceChannel", "The channel that you wish to monitor.")] ITextChannel sourceChannel,
            [Help("destChannel", "The channel of the server where you want the messages to appear.")] ITextChannel destChannel,
            [Help("date", "The optional date-time for the first post to synchronise.", "example: \"2020-01-01 10:00 AM\"")] DateTime? fromDate = null)
        {
            if(!Permissions.IsBotOwner(Context))
            {
                await Context.ApplyResultReaction(CommandResult.FailedUserPermission).ConfigureAwait(false);
                return;
            }

            // Only allow using channels of the current guild.
            if (destChannel != null && destChannel.Guild != Context.Guild)
            {
                await Context.ApplyResultReaction(CommandResult.FailedUserPermission).ConfigureAwait(false);
                return;
            }

            if (sourceChannel != null)
            {
                var link = await LinkUtility.TryAddLinkAsync(LinkType.Discord, destChannel, sourceChannel.Id.ToString(), fromDate).ConfigureAwait(false);
                Log.Debug($"Added link {link.Id}: {sourceChannel.Id} -> {destChannel.Id}");
                await Context.ApplyResultReaction(link == null ? CommandResult.Failed : CommandResult.Success).ConfigureAwait(false);
            }
            else
            {
                await Context.ApplyResultReaction(CommandResult.FailedBotPermission).ConfigureAwait(false);
            }
        }
    }
}
