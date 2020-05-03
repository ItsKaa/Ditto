using Discord;
using Discord.Commands;
using Ditto.Bot.Database.Data;
using Ditto.Bot.Database.Models;
using Ditto.Data.Discord;
using Ditto.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ditto.Bot.Modules.Utility.Linking
{
    [Alias("link")]
    public class LinkUtility : DiscordModule<Utility>
    {
        private static ConcurrentDictionary<int, Link> _links;
        private static CancellationTokenSource _cancellationTokenSource;
        private static ConcurrentDictionary<LinkType, Func<Link, ITextChannel, Task<IEnumerable<string>>>> _typeParsingHandlers
            = new ConcurrentDictionary<LinkType, Func<Link, ITextChannel, Task<IEnumerable<string>>>>();

        public static bool TryAddHandler(LinkType linkType, Func<Link, ITextChannel, Task<IEnumerable<string>>> func)
        {
            return _typeParsingHandlers.TryAdd(linkType, func);
        }


        static LinkUtility()
        {
            //On client connected
            Ditto.Connected += () =>
            {
                _links?.Clear();
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource = new CancellationTokenSource();
                Ditto.Database.ReadAsync((uow) =>
                {
                    _links = new ConcurrentDictionary<int, Link>(
                        uow.Links.GetAllWithLinks()
                        .Select(i => new KeyValuePair<int, Link>(i.Id, i))
                    );
                });

                var _ = Task.Run(async () =>
                {
                    while (Ditto.Running)
                    {
                        var databaseModified = false;
                        var tasks = new List<Task>();
                        var links = _links.Select(i => i.Value).ToList();
                        foreach (var link in links)
                        {
                            if (!(Ditto.Running && await Ditto.IsClientConnectedAsync()))
                            {
                                break;
                            }

                            tasks.Add(Task.Run(async () =>
                            {
                                // Verify that we have access to the channel
                                var channel = link.Channel;
                                if (channel != null
                                    && channel.GuildId == link.GuildId
                                    && (await Ditto.Client.DoAsync((c) => c.GetPermissionsAsync(channel))).HasAccess()
                                   )
                                {
                                    var linkItems = await ReadAndPostLinkAsync(link).ConfigureAwait(false);
                                    if (linkItems.Count() > 0)
                                    {
                                        link.Links.AddRange(linkItems);
                                        databaseModified = true;
                                    }
                                }
                                else
                                {
                                    Log.Debug($"Could not access the channel {channel.Guild?.Name}:{channel.Name}");
                                }
                            }));
                        }

                        await Task.WhenAll(tasks).ConfigureAwait(false);
                        if (databaseModified)
                        {
                            await Ditto.Database.WriteAsync(uow =>
                            {
                                uow.Links.UpdateRange(links);
                            });
                        }


                        await Task.Delay(500);
                    }
                });
                return Task.CompletedTask;
            };

            // On client disconnected
            Ditto.Exit += () =>
            {
                _links?.Clear();
                _cancellationTokenSource?.Cancel();
                return Task.CompletedTask;
            };
        }

        private static async Task<IEnumerable<LinkItem>> ReadAndPostLinkAsync(Link link)
        {
            if (_typeParsingHandlers.TryGetValue(link.Type, out Func<Link, ITextChannel, Task<IEnumerable<string>>> func))
            {
                return (await func(link, link.Channel).ConfigureAwait(false))
                    .Select(i => new LinkItem()
                    {
                        Link = link,
                        Identity = i
                    });
            }
            return Enumerable.Empty<LinkItem>();
        }

        public static async Task<bool> SendRetryLinkAsync(LinkType linkType, int repostCount, Exception ex = null)
        {
            if (Ditto.Running && await Ditto.IsClientConnectedAsync())
            {
                Log.Warn($"Attempting to repost link ({linkType.ToString()})... {repostCount}{(ex == null ? string.Empty : $" | {ex}")}");
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
