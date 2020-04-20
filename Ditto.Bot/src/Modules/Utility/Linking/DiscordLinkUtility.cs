using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Ditto.Attributes;
using Ditto.Bot.Database.Data;
using Ditto.Bot.Modules.Admin;
using Ditto.Data.Commands;
using Ditto.Data.Discord;
using Ditto.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ditto.Bot.Modules.Utility.Linking
{
    [Alias("discord")]
    public class DiscordLinkUtility : DiscordModule<LinkUtility>
    {
        private static DiscordClientEx _discordClient = null;
        private static ConcurrentDictionary<int, DateTime> LastUpdate { get; set; }
        private static volatile bool _connected;
        private static volatile bool _reconnecting;
        private static volatile bool _initialLogin;

        static DiscordLinkUtility()
        {
            LastUpdate = new ConcurrentDictionary<int, DateTime>();
            _reconnecting = false;
            _initialLogin = true;

            _discordClient = new DiscordClientEx(new DiscordSocketConfig()
            {
                MessageCacheSize = Ditto.Settings.Cache.AmountOfCachedMessages,
                LogLevel = LogSeverity.Warning,
                ConnectionTimeout = (int)(Ditto.Settings.Timeout * 60),
                HandlerTimeout = (int)(Ditto.Settings.Timeout * 60),
                DefaultRetryMode = RetryMode.AlwaysRetry,
            });

            var loginAction = new Func<Task>(async () =>
            {
                // Cancel out when already reconnecting
                if (_reconnecting)
                {
                    return;
                }

                if(!_initialLogin)
                {
                    Log.Info("Reconnecting discord slave user...");
                }

                // Attempt to continuously reconnect.
                _reconnecting = true;
                _connected = false;
                int retryAttempt = 0;
                while (true)
                {
                    try
                    {
                        await _discordClient.LoginAsync(0, Ditto.Settings.Credentials.UserSlaveToken, true);
                        await _discordClient.StartAsync().ConfigureAwait(false);
                        _initialLogin = false;
                        _reconnecting = false;
                        break;
                    }
                    catch
                    {
                        if (_initialLogin)
                        {
                            _initialLogin = false;
                            Log.Error("Slave user could not connect at first login, will not retry further.");
                            break;
                        }
                        else
                        {
                            _initialLogin = false;
                            Log.Warn($"Slave user could not connect ({++retryAttempt})");
                            await Task.Delay(500).ConfigureAwait(false);
                        }
                    }
                }
            });


            _discordClient.Connected += () =>
            {
                _connected = true;
                Log.Info("Slave user connected!");
                return Task.CompletedTask;
            };

            _discordClient.Disconnected += (ex) =>
            {
                if (!_initialLogin)
                {
                    Log.Warn($"Slave user has been disconnected.");
                    Task.Run(() => loginAction());
                }
                return Task.CompletedTask;
            };

            _discordClient.LoggedOut += () =>
            {
                if (!_initialLogin)
                {
                    Log.Warn($"Slave user got logged out.");
                    Task.Run(() => loginAction());
                }
                return Task.CompletedTask;
            };

            Task.Run(async () =>
            {
                await loginAction().ConfigureAwait(false);
            });

            LinkUtility.TryAddHandler(LinkType.Discord, async (link, channel) =>
            {
                var messageIds = new List<string>();

                // Only pull discord channel feeds every 2 minutes for each individual channel.
                var lastUpdateTime = LastUpdate.GetOrAdd(link.Id, DateTime.MinValue);
                if (!_connected || (DateTime.UtcNow - lastUpdateTime).TotalSeconds < 120)
                {
                    return messageIds;
                }

                if (link != null)
                {
                    if (ulong.TryParse(link.Value, out ulong linkChannelId))
                    {
                        if (_discordClient.GetChannel(linkChannelId) is ITextChannel linkChannel)
                        {
                            // Retrieve the latest messages in bulk from the targeted channel.
                            var messages = (await linkChannel.GetMessagesAsync(100, CacheMode.AllowDownload).ToListAsync().ConfigureAwait(false))
                                .SelectMany(m => m)
                                .Where(m => m.CreatedAt.UtcDateTime >= link.Date)
                                .Where(m => null == link.Links.FirstOrDefault(l => l.Identity == m.Id.ToString()))
                                ;

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
                                foreach (var message in messages.OrderBy(m => m.CreatedAt))
                                {
                                    int retryCount = 0;
                                    while (retryCount < 10)
                                    {
                                        try
                                        {
                                            var dateUtc = message.CreatedAt.UtcDateTime;
                                            var embedBuilder = new EmbedBuilder()
                                                .WithAuthor(message.Author)
                                                .WithTitle(message.Channel.Name)
                                                .WithDescription(message.Content)
                                                .WithFooter($"Posted {dateUtc:dddd, MMMM} {dateUtc.Day.Ordinal()} {dateUtc:yyyy} at {dateUtc:HH:mm} UTC")
                                                .WithDiscordLinkColour(channel.Guild)
                                                ;

                                            if (message.Attachments.Count > 0)
                                            {
                                                embedBuilder.WithImageUrl(message.Attachments.FirstOrDefault().Url);
                                            }

                                            var postedMessage = await channel.SendMessageAsync(embed: embedBuilder.Build(), options: new RequestOptions() { RetryMode = RetryMode.AlwaysFail }).ConfigureAwait(false);
                                            if (postedMessage != null)
                                            {
                                                messageIds.Add(message.Id.ToString());
                                                lastMessageDate = message.CreatedAt.UtcDateTime;
                                            }

                                            // OK, cancel out.
                                            break;
                                        }
                                        catch (Exception ex)
                                        {
                                            // Update the link date just in case.
                                            await funcUpdateLinkDate().ConfigureAwait(false);

                                            // Attempt to retry sending the message
                                            if (!await LinkUtility.SendRetryLinkAsync(link.Type, retryCount++, ex is Discord.Net.RateLimitedException ? null : ex))
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
                    }
                }

                LastUpdate.TryUpdate(link.Id, DateTime.UtcNow, lastUpdateTime);
                return messageIds;
            });
        }

        [DiscordCommand(CommandSourceLevel.Guild, CommandAccessLevel.Local)]
        [Alias("add", "link", "hook", "register")]
        public async Task Add(
            [Help("channel", "The guild channel of this server.")] ITextChannel textChannel,
            [Help("linkChannelId", "The ID of the channel from the other server that you wish to link to the selected 'channel'.", "example: 334120131626621412")] ulong linkChannelId,
            [Help("date", "The optional date-time for the first post to synchronise.", "example: \"2020-01-01 10:00 AM\"")] DateTime? fromDate = null)
        {
            if(!Permissions.IsBotOwner(Context))
            {
                await Context.ApplyResultReaction(CommandResult.FailedUserPermission).ConfigureAwait(false);
                return;
            }

            var linkChannel = _discordClient.GetChannel(linkChannelId) as ITextChannel;
            if(textChannel == null || linkChannel == null)
            {
                await Context.ApplyResultReaction(CommandResult.FailedBotPermission).ConfigureAwait(false);
                return;
            }

            var link = await LinkUtility.TryAddLinkAsync(LinkType.Discord, textChannel, linkChannel.Id.ToString(), fromDate).ConfigureAwait(false);
            Log.Debug($"Added link {link.Id}: {linkChannelId} -> {textChannel.Id}");
            await Context.ApplyResultReaction(link == null ? CommandResult.Failed : CommandResult.Success).ConfigureAwait(false);
        }
    }
}
