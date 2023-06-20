using Discord;
using Discord.Commands;
using Ditto.Bot.Database.Data;
using Ditto.Bot.Database.Models;
using Ditto.Bot.Services;
using Ditto.Data.Discord;
using Ditto.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ditto.Bot.Modules.Utility.Linking
{
    [Alias("link")]
    public class LinkUtility : DiscordTextModule<Utility>
    {
        private static ConcurrentDictionary<int, Link> _links;
        private static CancellationTokenSource _cancellationTokenSource;
        private static ConcurrentDictionary<LinkType, Func<Link, ITextChannel, CancellationToken, Task<IEnumerable<string>>>> _typeParsingHandlers
            = new ConcurrentDictionary<LinkType, Func<Link, ITextChannel, CancellationToken, Task<IEnumerable<string>>>>();

        public static bool TryAddHandler(LinkType linkType, Func<Link, ITextChannel, CancellationToken, Task<IEnumerable<string>>> func)
        {
            return _typeParsingHandlers.TryAdd(linkType, func);
        }


        static LinkUtility()
        {
            //On client connected
            Ditto.Connected += async () =>
            {
                _links?.Clear();
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource = new CancellationTokenSource();
                await Ditto.Database.ReadAsync((uow) =>
                {
                    _links = new ConcurrentDictionary<int, Link>(
                        uow.Links.GetAllWithLinks()
                        .Select(i => new KeyValuePair<int, Link>(i.Id, i))
                    );
                }).ConfigureAwait(false);

                var _ = Task.Run(async () =>
                {
                    while (Ditto.Running)
                    {
                        var tasks = new List<Task>();
                        var links = _links.Select(i => i.Value).ToList();
                        foreach (var link in links)
                        {
                            if (!(Ditto.Running && Ditto.IsClientConnected()))
                            {
                                break;
                            }

                            var mutex = new SemaphoreSlim(1, 1);
                            tasks.Add(Task.Run(async () =>
                            {
                                try
                                {
                                    // Verify that we have access to the channel
                                    var channel = link.Channel;
                                    if (channel != null
                                        && channel.GuildId == link.GuildId
                                        && (await Ditto.Client.GetPermissionsAsync(channel)).HasAccess()
                                       )
                                    {
                                        var linkItems = await ReadAndPostLinkAsync(link).ConfigureAwait(false);
                                        if (linkItems.Count() > 0)
                                        {
                                            link.Links.AddRange(linkItems);
                                            try
                                            {
                                                await mutex.WaitAsync().ConfigureAwait(false);
                                                await Ditto.Database.WriteAsync(uow =>
                                                {
                                                    uow.Links.Update(link);
                                                }, throwOnError: true);

                                                await Task.Delay(10).ConfigureAwait(false);
                                            }
                                            catch (Exception ex)
                                            {
                                                Log.Error("Failed to update link");
                                                Log.Error(ex);
                                            }
                                            finally
                                            {
                                                mutex.Release();
                                            }
                                        }
                                    }
                                    else
                                    {
                                        Log.Debug($"Could not access the channel {channel?.Guild?.Name}:{channel?.Name}");
                                    }
                                }
                                catch(Exception ex)
                                {
                                    Log.Error("Error at link handler", ex);
                                }
                            }));
                        }

                        try
                        {
                            await Task.WhenAll(tasks).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            Log.Error("Error at waiting for link handlers", ex);
                        }

                        await Task.Delay(500).ConfigureAwait(false);
                    }
                });
            };

            // On client disconnected
            Ditto.Exit += () =>
            {
                _links?.Clear();
                _cancellationTokenSource?.Cancel();
                return Task.CompletedTask;
            };
        }

        public LinkUtility(DatabaseCacheService cache, DatabaseService database) : base(cache, database)
        {
        }

        private static async Task<IEnumerable<LinkItem>> ReadAndPostLinkAsync(Link link)
        {
            if (_typeParsingHandlers.TryGetValue(link.Type, out Func<Link, ITextChannel, CancellationToken, Task<IEnumerable<string>>> func))
            {
                return (await func(link, link.Channel, _cancellationTokenSource.Token).ConfigureAwait(false))
                    .Select(i => new LinkItem()
                    {
                        Link = link,
                        Identity = i
                    });
            }
            return Enumerable.Empty<LinkItem>();
        }

        public static async Task<bool> SendRetryLinkMessageAsync(LinkType linkType, int repostCount, Exception ex = null)
        {
            if (Ditto.Running && Ditto.IsClientConnected())
            {
                Log.Debug($"Attempting to repost link ({linkType.ToString()})... {repostCount}{(ex == null ? string.Empty : $" | {ex}")}");
                await Task.Delay(1000).ConfigureAwait(false);
                return true;
            }
            return false;
        }

        public static async Task<Link> TryAddLinkAsync(LinkType linkType, ITextChannel textChannel, string value, Func<string, string, bool> comparer, DateTime? date = null)
        {
            // Check if value already exists
            if (!(await Ditto.Database.ReadAsync(uow =>
            {
                return uow.Links.GetAll(i =>
                    i.GuildId == textChannel.GuildId
                    && i.ChannelId == textChannel.Id
                )
                .Where(i => comparer(i.Value, value))
                .Count() > 0;
            }).ConfigureAwait(false)))
            {
                Link link = null;
                await Ditto.Database.WriteAsync((uow) =>
                {
                    link = uow.Links.Add(new Link()
                    {
                        Type = linkType,
                        Value = value,
                        Guild = textChannel.Guild,
                        Channel = textChannel,
                        Date = date ?? DateTime.UtcNow,
                    });
                });

                if (link != null)
                {
                    _links?.TryAdd(link.Id, link);
                }
                return link;
            }
            return null;
        }

        public static Task<Link> TryAddLinkAsync(LinkType linkType, ITextChannel textChannel, string value, DateTime? date = null)
            => TryAddLinkAsync(linkType, textChannel, value, ((left, right) => Equals(left, right)), date);
        
        public static bool LinkItemExists(Link link, string value, StringComparison stringComparison = StringComparison.CurrentCulture)
            => link.Links.Exists(item => item.Identity.Equals(value, stringComparison));
    }
}
