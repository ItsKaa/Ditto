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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Ditto.Bot.Modules.Utility.Linking
{
    [Alias("sololeveling", "sl")]
    public class SoloLevelingUtility : DiscordModule<LinkUtility>
    {
        private static ConcurrentList<Link> _links = new ConcurrentList<Link>();
        private static SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);
        private static readonly Regex _regexGateClosed = new Regex(@"(?:GATE CLOSED: )", RegexOptions.Compiled);
        private static readonly Regex _regexMonsterSlain = new Regex(@"(?:AN ENEMY)(?:\s*)?([\w\W]*)?(?:HAS BEEN SLAIN)", RegexOptions.Compiled);

        static SoloLevelingUtility()
        {
            Ditto.Connected += async () =>
            {
                // Recheck the valid links
                _links.Clear();
                await Ditto.Database.ReadAsync(uow =>
                {
                    _links.AddRange(uow.Links.GetAllWithLinks(l => l.Type == LinkType.SoloLeveling));
                }).ConfigureAwait(false);

                // TODO: Recalculate number of messages
                foreach (var link in _links)
                {
                    var messages = new List<IMessage>();
                    var lastRecordedDate = GetLastRecordedDateFromLink(link);
                    ulong lastMessageId = ulong.MaxValue;
                    while (true)
                    {
                        var messagesChunk = Enumerable.Empty<IMessage>();
                        if (lastMessageId != ulong.MaxValue)
                        {
                            messagesChunk = (await link.Channel.GetMessagesAsync(lastMessageId, Direction.Before, 100, CacheMode.AllowDownload).ToListAsync().ConfigureAwait(false))
                                .SelectMany(m => m)
                                .Where(m => m.CreatedAt.UtcDateTime > link.Date);
                        }
                        else
                        {
                            messagesChunk = (await link.Channel.GetMessagesAsync(100, CacheMode.AllowDownload).ToListAsync().ConfigureAwait(false))
                                .SelectMany(m => m)
                                .Where(m => m.CreatedAt.UtcDateTime > link.Date);
                        }

                        messages.AddRange(messagesChunk);
                        lastMessageId = messagesChunk.LastOrDefault()?.Id ?? ulong.MaxValue;

                        // Cancel when the last recorded message is larger than the earliest message date of the collection, meaning we're good to go.
                        if (lastRecordedDate > messages.Min(x => x.CreatedAt.UtcDateTime))
                        {
                            messages = messages.From(x => x.CreatedAt.UtcDateTime > lastRecordedDate).ToList();
                            break;
                        }

                        // Cancel when message count is less than the maximum.
                        if (messagesChunk.Count() < 100)
                        {
                            break;
                        }
                    }

                    // Process any missed messages
                    if (messages.Count > 0)
                    {
                        foreach (var message in messages)
                        {
                            await ProcessMessageAsync(message).ConfigureAwait(false);
                        }
                    }
                }

                // Start listening to new messages
                await Ditto.Client.DoAsync(client =>
                {
                    client.MessageReceived += OnMessageReceived;
                }).ConfigureAwait(false);
            };

            Ditto.Exit += async () =>
            {
                await Ditto.Client.DoAsync(client =>
                {
                    client.MessageReceived -= OnMessageReceived;
                }).ConfigureAwait(false);
            };
        }

        private static T GetValueFromLink<T>(Link link, int index, T defaultValue = default)
        {
            var stringValues = link.Value.Split('|', StringSplitOptions.RemoveEmptyEntries);
            if (stringValues.Length >= index)
            {
                try
                {
                    var converter = TypeDescriptor.GetConverter(typeof(T));
                    if (converter != null)
                    {
                        return (T)converter.ConvertFromString(stringValues[index]);
                    }
                }
                catch (NotSupportedException)
                {
                    return defaultValue;
                }
            }

            return defaultValue;
        }
        private static ulong GetUserIdFromLink(Link link)
        {
            return GetValueFromLink<ulong>(link, 0);
        }

        private static async Task<IUser> GetUserFromLinkAsync(Link link)
        {
            var userId = GetUserIdFromLink(link);
            if (userId > 0)
            {
                return (await Ditto.Client.DoAsync(client =>
                {
                    return client.GetUser(userId);
                }).ConfigureAwait(false));
            }

            return null;
        }

        private static int GetPermittedNumberOfKillsFromLink(Link link)
        {
            return GetValueFromLink<int>(link, 1);
        }

        private static int GetRecordedNumberOfKillsFromLink(Link link)
        {
            return GetValueFromLink<int>(link, 2);
        }

        private static DateTime GetLastRecordedDateFromLink(Link link)
        {
            var ticks = GetValueFromLink<long>(link, 3);
            if (ticks > 0)
            {
                return new DateTime(ticks, DateTimeKind.Utc);
            }

            return DateTime.MinValue;
        }

        private static string FormatValueToString(IUser user, int numberOfPermittedKills, int recordedNumberOfKills, DateTime lastDateTime)
        {
            return $"{user.Id}|{numberOfPermittedKills}|{recordedNumberOfKills}|{lastDateTime.Ticks}";
        }

        private static async Task<bool> PermitUserToChannel(IUser user, ITextChannel channel, int numberOfKills = 0)
        {
            var dateTime = DateTime.UtcNow;

            // Add entry to database
            Link link = null;
            var writtenToDatabase = await Ditto.Database.WriteAsync(uow =>
            {
                link = uow.Links.Add(new Link()
                {
                    Channel = channel,
                    Guild = channel.Guild,
                    Date = dateTime,
                    Type = LinkType.SoloLeveling,
                    Value = FormatValueToString(user, numberOfKills, 0, dateTime),
                });

                if (link != null)
                {
                    _links.Add(link);
                    return true;
                }
                return false;
            });

            if (!writtenToDatabase)
            {
                return false;
            }

            // Reset to the parent permissions
            await channel.SyncPermissionsAsync().ConfigureAwait(false);

            // Update the channel permissions
            await channel.AddPermissionOverwriteAsync(user, new OverwritePermissions(viewChannel: PermValue.Allow, sendMessages: PermValue.Allow, manageRoles: PermValue.Allow), new RequestOptions() { RetryMode = RetryMode.AlwaysRetry });

            // Add message to the channel pinging the user.
            await channel.SendMessageAsync(
                $"{user.Mention} You now have access to this channel, you are able to edit this channel to grant other users access.\n" +
                $"{channel.Mention} will be monitored for the amount of kills in and the access will be revoked when {(numberOfKills > 0 ? $"you have killed {numberOfKills} monsters" : "the gate is closed")}."
            ).ConfigureAwait(false);

            return true;
        }

        private static async Task UnpermitLinkAsync(Link link)
        {
            var user = await GetUserFromLinkAsync(link).ConfigureAwait(false);
            var numberOfPermittedKills = GetPermittedNumberOfKillsFromLink(link);

            // Reset to the parent permissions
            await link.Channel.SyncPermissionsAsync().ConfigureAwait(false);

            // Remove entry from database
            await Ditto.Database.WriteAsync(uow =>
            {
                uow.Links.Remove(link);
            });
            _links.Remove(link);
        }

        private static async Task ProcessMessageAsync(IMessage message)
        {
            if (message.Author.IsBot && message.Channel is ITextChannel textChannel)
            {
                var embed = message.Embeds.FirstOrDefault();
                if (embed != null)
                {
                    var links = _links.Where(x => x.Channel == textChannel);
                    if (links.Count() > 0)
                    {
                        if (_regexGateClosed.IsMatch(embed.Description))
                        {
                            foreach (var link in links)
                            {
                                var user = await GetUserFromLinkAsync(link).ConfigureAwait(false);
                                await _semaphoreSlim.DoAsync(async () =>
                                {
                                    await link.Channel.SendMessageAsync($"{user.Mention} has cleared this gate, cleared the custom channel permissions.").ConfigureAwait(false);
                                    await UnpermitLinkAsync(link).ConfigureAwait(false);
                                });
                            }
                        }
                        else if (_regexMonsterSlain.IsMatch(embed.Description))
                        {
                            foreach (var link in links)
                            {
                                await _semaphoreSlim.DoAsync(async () =>
                                {
                                    var user = await GetUserFromLinkAsync(link).ConfigureAwait(false);
                                    if (user != null)
                                    {
                                        try
                                        {
                                            var numberOfPermittedKills = GetPermittedNumberOfKillsFromLink(link);
                                            var numberOfRecordedKills = GetRecordedNumberOfKillsFromLink(link) + 1;
                                            var createdDate = message?.CreatedAt.UtcDateTime ?? DateTime.UtcNow;
                                            link.Value = FormatValueToString(user, numberOfPermittedKills, (numberOfRecordedKills), createdDate);

                                            if (numberOfPermittedKills > 0 && numberOfRecordedKills > numberOfPermittedKills)
                                            {
                                                await link.Channel.SendMessageAsync($"{user.Mention} has killed {numberOfRecordedKills} monsters, cleared the custom channel permissions.").ConfigureAwait(false);
                                                await UnpermitLinkAsync(link).ConfigureAwait(false);
                                            }
                                            else
                                            {
                                                await Ditto.Database.WriteAsync(uow =>
                                                {
                                                    uow.Links.Update(link);
                                                }).ConfigureAwait(false);
                                            }

                                            // Logging message in case we need it.
                                            Log.Info($"[SoloLeveling][{link.Channel.Name}][{user.Username}#{user.Discriminator}]: Killed {numberOfRecordedKills}/{numberOfPermittedKills}");
                                            //var _ = Task.Run(() =>
                                            //{
                                            //    link.Channel.SendMessageAsync($"[Debug] You killed {numberOfRecordedKills} out of {(numberOfPermittedKills == 0 ? "\\♾️" : $"{numberOfPermittedKills}")} monsters.");
                                            //});
                                        }
                                        catch (Exception ex)
                                        {
                                            Log.Error($"[SoloLeveling] Exception thrown: {ex}");
                                        }
                                    }
                            }).ConfigureAwait(false);
                            }
                        }
                    }
                }
            }
            }

        private static Task OnMessageReceived(Discord.WebSocket.SocketMessage socketMessage)
        {
            var _ = Task.Run(async () =>
            {
                await ProcessMessageAsync(socketMessage).ConfigureAwait(false);
            });
            return Task.CompletedTask;
        }

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.All)]
        public async Task Permit(IUser user, ITextChannel channel, int numberOfKills = 0)
        {
            if (!Permissions.IsAdministratorOrBotOwner(Context))
            {
                await Context.ApplyResultReaction(CommandResult.FailedUserPermission).ConfigureAwait(false);
                return;
            }

            if (!(await Permissions.CanBotManageChannel(channel).ConfigureAwait(false)))
            {
                await Context.ApplyResultReaction(CommandResult.FailedBotPermission).ConfigureAwait(false);
                return;
            }

            // Find existing permit with the provided values.
            Link foundLink = null;
            foreach(var link in _links)
            {
                if (link.Guild == channel.Guild && link.Channel == channel)
                {
                    var userId = GetUserIdFromLink(link);
                    if (Context.User?.Id == userId)
                    {
                        foundLink = link;
                        break;
                    }
                }
            }

            if(foundLink != null)
            {
                // TODO: Update existing link?
                await Context.EmbedAsync(ContextMessageOption.ReplyWithError, "That user already has permission to access this channel, it's currently not supported to override or update existing permissions, please remove the permission first.");
                await Context.ApplyResultReaction(CommandResult.Failed).ConfigureAwait(false);
            }
            else
            {
                // Create link
                if (await PermitUserToChannel(user, channel, numberOfKills).ConfigureAwait(false))
                {
                    await Context.ApplyResultReaction(CommandResult.Success).ConfigureAwait(false);
                }
                else
                {
                    await Context.EmbedAsync(ContextMessageOption.ReplyWithError, "An unknown error occured while trying to create a user permission.");
                    await Context.ApplyResultReaction(CommandResult.Failed).ConfigureAwait(false);
                }
            }
        }

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.All)]
        public async Task Unpermit(IUser user, ITextChannel channel)
        {
            if (!Permissions.IsAdministratorOrBotOwner(Context))
            {
                await Context.ApplyResultReaction(CommandResult.FailedUserPermission).ConfigureAwait(false);
                return;
            }

            if (!(await Permissions.CanBotManageChannel(channel).ConfigureAwait(false)))
            {
                await Context.ApplyResultReaction(CommandResult.FailedBotPermission).ConfigureAwait(false);
                return;
            }

            var links = _links.Where(x => x.Channel == channel && GetUserIdFromLink(x) == user.Id);
            if (links.Count() > 0)
            {
                foreach (var link in links)
                {
                    await link.Channel.SendMessageAsync($"Access has been revoked for {user.Mention}, cleared the custom channel permissions.").ConfigureAwait(false);
                    await UnpermitLinkAsync(link).ConfigureAwait(false);
                }
                await Context.ApplyResultReaction(CommandResult.Success);
            }
            else
            {
                await Context.ApplyResultReaction(CommandResult.SuccessAlt1);
            }
        }

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.All)]
        public Task Permit(ITextChannel channel, IUser user, int numberOfKills = 0)
            => Permit(user, channel, numberOfKills);

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.All)]
        public Task Unpermit(ITextChannel channel, IUser user)
            => Unpermit(user, channel);

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.All)]
        [Alias("stats")]
        [Priority(3)]
        public async Task Statistics(IUser user, ITextChannel textChannel)
        {
            List<Link> links = _links.ToList();
            // Filter on user
            if (user != null)
            {
                links = links.Where(x => GetUserIdFromLink(x) == user.Id).ToList();
            }
            // Filter on channel
            if (textChannel != null)
            {
                links = links.Where(x => x.Channel == textChannel).ToList();
            }

            // Get the descriptions for each link
            var descriptions = new List<string>();
            foreach (var link in links)
            {
                var linkUser = await GetUserFromLinkAsync(link).ConfigureAwait(false);
                var numberOfPermittedKills = GetPermittedNumberOfKillsFromLink(link);
                var numberOfRecordedKills = GetRecordedNumberOfKillsFromLink(link);
                descriptions.Add($"• {link.Channel.Mention} in use by {linkUser.Mention}: {numberOfRecordedKills} out of {(numberOfPermittedKills == 0 ? "\\♾️" : $"{numberOfPermittedKills}")} monsters defeated.");
            }

            // Get a chunk of 10 of the descriptions, max allowed should be 19/20.
            var descriptionChunks = descriptions.ChunkBy(10);
            var getEmbedForPage = new Func<int, EmbedBuilder>((page) =>
            {
                var descriptionChunk = descriptionChunks.ElementAtOrDefault(page - 1);
                if (descriptionChunk == null)
                {
                    descriptionChunk = new[] { "No active channels found." };
                }

                return new EmbedBuilder()
                        .WithTitle($"\\🌸 Solo Leveling - Statistics")
                        .WithDescription(string.Join('\n', descriptionChunk));
            });

            // Either post a list with reactions or the full embed.
            if (descriptionChunks.Count() > 1)
            {
                await Context.SendPagedMessageAsync(
                    getEmbedForPage(1),
                    (message, page) => getEmbedForPage(page),
                    descriptionChunks.Count()
                ).ConfigureAwait(false);
            }
            else
            {
                await Context.EmbedAsync(getEmbedForPage(1)).ConfigureAwait(false);
            }
        }

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.All)]
        [Alias("stats")]
        [Priority(2)]
        public Task Statistics(ITextChannel textChannel, IUser user)
            => Statistics(user, textChannel);

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.All)]
        [Alias("stats")]
        [Priority(1)]
        public Task Statistics(ITextChannel textChannel)
            => Statistics(null, textChannel);

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.All)]
        [Alias("stats")]
        [Priority(0)]
        public Task Statistics(IUser user = null)
            => Statistics(user, null);
    }
}
