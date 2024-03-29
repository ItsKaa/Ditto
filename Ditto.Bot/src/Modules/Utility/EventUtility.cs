﻿using Discord;
using Discord.Commands;
using Ditto.Attributes;
using Ditto.Bot.Database.Models;
using Ditto.Bot.Modules.Admin;
using Ditto.Bot.Modules.Utility.Data;
using Ditto.Data;
using Ditto.Data.Commands;
using Ditto.Data.Discord;
using Ditto.Extensions;
using Ditto.Helpers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Ditto.Bot.Modules.Utility
{
    [Alias("event", "events")]
    [Help("Event", "")]
    public class EventUtility : DiscordModule
    {

        private static ConcurrentDictionary<int, Event> _events = new ConcurrentDictionary<int, Event>();
        private static CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        static EventUtility()
        {
            Ditto.Exit += () =>
            {
                _cancellationTokenSource?.Cancel();
                _events.Clear();
                return Task.CompletedTask;
            };

            Ditto.Connected += () =>
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource = new CancellationTokenSource();

                // Add every event on load
                IEnumerable<Event> eventsDb = null;
                Ditto.Database.Read((uow) =>
                {
                    eventsDb = uow.Events.GetAll();
                });

                foreach (var item in eventsDb)
                {
                    _events.TryAdd(item.Id, item);
                }

                // Start reading events
                Task.Run(async () =>
                {
                    while (Ditto.Running)
                    {
                        var timeTolerance = TimeSpan.FromMinutes(5);
                        var dateNow = DateTime.UtcNow;
                        foreach (var pair in _events.ToList())
                        {
                            var @event = pair.Value;
                            // Reversed offset due to it noting the timezone
                            var timeOffset = @event.TimeOffset ?? TimeSpan.FromSeconds(0);
                            var timeBegin = @event.TimeBegin - timeOffset;
                            var timeEnd = (@event.TimeEnd ?? @event.TimeBegin) - timeOffset;

                            // Calculate the time difference with the subtracted time to allow displaying the message at a set time before the event begins.
                            // TODO: Fix for older/now messages and include a check for the ending time in case a restart happened.
                            var timeCountdown = pair.Value.TimeCountdown ?? TimeSpan.FromSeconds(0);
                            var timeDiffWithCountdown = ((dateNow.TimeOfDay + timeCountdown) - timeBegin);

                            if ((dateNow - (@event.LastRun ?? DateTime.MinValue)).TotalHours >= 1
                               && DateHelper.HasDayOfWeek(@event.Days, dateNow.DayOfWeek)
                               && timeDiffWithCountdown.TotalSeconds > 0 && timeDiffWithCountdown <= (timeTolerance)
                              )
                            {
                                // Verify that the reminder has not already been removed
                                Event eventDb = null;
                                await Ditto.Database.ReadAsync((uow) =>
                                {
                                    eventDb = uow.Events.Get(pair.Key);
                                });

                                // If the value is no longer in our database, remove it from our local records.
                                if (eventDb == null)
                                {
                                    _events.TryRemove(pair.Key, out Event value);
                                    continue;
                                }

                                // if for some reason the database version differs, update our local value.
                                else if (!eventDb.Equals(@event))
                                {
                                    _events.TryUpdate(pair.Key, eventDb, @event);
                                    continue;
                                }

                                // Event start
                                if (@event.Channel is ITextChannel textChannel)
                                {
                                    try
                                    {
                                        // Post event in the correct channel
                                        var embedBuilder = new EmbedBuilder()
                                            .WithAuthor(@event.Title)
                                            .WithDescription(@event.MessageBody)
                                            ;

                                        if (timeEnd != timeBegin)
                                        {
                                            embedBuilder.Footer = new EmbedFooterBuilder().WithText(@event.MessageFooter ?? $"⏰ Expires at {(timeEnd + timeOffset):hh\\:mm}");
                                        }

                                        if (null != await textChannel.EmbedAsync(@event.MessageHeader ?? string.Empty, embedBuilder).ConfigureAwait(false))
                                        {
                                            // Update database entry
                                            await Ditto.Database.WriteAsync((uow) =>
                                            {
                                                eventDb = uow.Events.Get(pair.Key);
                                                if (eventDb != null)
                                                {
                                                    eventDb.LastRun = dateNow;
                                                }
                                            }).ConfigureAwait(false);

                                            // Update collection
                                            if (_events.TryGetValue(pair.Key, out Event value))
                                            {
                                                value.LastRun = dateNow;
                                            }
                                        }
                                    }
                                    catch { }
                                }
                            }
                        }

                        await Task.Delay(500, _cancellationTokenSource.Token).ConfigureAwait(false);
                    }
                }, _cancellationTokenSource.Token);

                return Task.CompletedTask;
            };
        }

        [DiscordCommand(CommandSourceLevel.Guild, CommandAccessLevel.LocalAndParents)]
        [Priority(5)]
        [Help(null, "Create an event that will trigger based on the specified day(s) and time.")]
        public async Task Add(
            [Help("channel", "The targeted text channel")]
            ITextChannel channel,
            [Help("day", "Days separated by a comma", "Possible values: %values%")]
            EventDay day,
            [Help("time", "Formatted time span in UTC", "Examples: '19:30~20:00' or '19:30~20:00+01:00' for UTC+1h")]
            string timeMessage,
            [Help("title", "The event title")]
            [Optional] string title,
            [Help("header", "The message posted above the embedded event block, can be used for role pings.")]
            [Optional] string header,
            [Help("message", "The body message of the event, this accepts multi-lined strings as well as markdown text.")]
            [Multiword] string message
            )
        {
            if(!Permissions.IsAdministratorOrBotOwner(Context))
            {
                await Context.ApplyResultReaction(CommandResult.FailedUserPermission).ConfigureAwait(false);
                return;
            }

            // Only allow using channels of the current guild.
            if (channel != null && channel.Guild != Context.Guild)
            {
                await Context.ApplyResultReaction(CommandResult.FailedUserPermission).ConfigureAwait(false);
                return;
            }

            string timeStringBegin = "", timeStringEnd = "", timeStringOffset = "", offsetSign = "+";

            var regex = Globals.RegularExpression.TimeMessage;
            var match = regex.Match(timeMessage);

            foreach (var groupName in regex.GetGroupNames())
            {
                var groupValue = match.Groups[groupName].Value;
                if(groupName == "time1")
                {
                    timeStringBegin = groupValue;
                }
                else if(groupName == "time2")
                {
                    timeStringEnd = groupValue;
                }
                else if(groupName == "sign")
                {
                    offsetSign = groupValue;
                }
                else if(groupName == "offset")
                {
                    timeStringOffset = groupValue;
                }
            }

            // Parse offset
            if (!TimeSpan.TryParse(timeStringOffset, out TimeSpan timeOffset))
            {
                timeOffset = new TimeSpan(0, 0, 0);
            }

            if (offsetSign == "-")
            {
                timeOffset = -timeOffset;
            }

            // Parse time spans
            if (!TimeSpan.TryParse(timeStringBegin, out TimeSpan timeBegin))
            {
                await Context.ApplyResultReaction(CommandResult.InvalidParameters).ConfigureAwait(false);
                return;
            }

            TimeSpan? timeEnd = null;
            if (TimeSpan.TryParse(timeStringEnd, out TimeSpan timeEndValue))
            {
                timeEnd = timeEndValue;
            }

            // Everything is OK, add it to the database
            Event @event = null;
            await Ditto.Database.WriteAsync((uow) =>
            {
                @event = uow.Events.Add(new Event()
                {
                    Guild = Context.Guild,
                    Channel = channel ?? Context.Channel,
                    Creator = Context.User,
                    CreatorName = Context.NicknameAndGlobalUsername,
                    Days = (Day)day,
                    Title = title,
                    MessageHeader = header,
                    MessageBody = message,
                    MessageFooter = null,
                    TimeBegin = timeBegin,
                    TimeEnd = timeEnd,
                    TimeCountdown = null,
                    TimeOffset = timeOffset,
                    LastRun = DateTime.MinValue
                });
            });

            if (@event == null)
            {
                // Unexpected error
                await Context.ApplyResultReaction(CommandResult.Failed).ConfigureAwait(false);
                await Context.EmbedAsync("An unexpected error occurred, please try again.", ContextMessageOption.ReplyWithError).ConfigureAwait(false);
            }
            else
            {
                _events.TryAdd(@event.Id, @event);
                await Context.ApplyResultReaction(CommandResult.Success).ConfigureAwait(false);
            }
        }

        [DiscordCommand(CommandSourceLevel.Guild, CommandAccessLevel.LocalAndParents)]
        [Priority(4)]
        public Task Add(ITextChannel channel, string timeMessage, [Optional] string title, [Optional] string header, [Multiword] string message)
            => Add(channel, EventDay.Daily, timeMessage, title, header, message);


        [DiscordCommand(CommandSourceLevel.Guild, CommandAccessLevel.LocalAndParents)]
        [Priority(3)]
        public Task Add(EventDay day, string timeMessage, [Optional] string title, [Optional] string header, [Multiword] string message)
            => Add(null, day, timeMessage, title, header, message);

        [DiscordCommand(CommandSourceLevel.Guild, CommandAccessLevel.LocalAndParents)]
        [Priority(0)]
        public Task Add(string timeMessage, [Optional] string title, [Optional] string header, [Multiword] string message)
            => Add(null, EventDay.Daily, timeMessage, title, header, message);

        [DiscordCommand(CommandSourceLevel.Guild, CommandAccessLevel.LocalAndParents)]
        public async Task List(ITextChannel channel = null, IUser user = null)
        {
            if (!Permissions.IsAdministratorOrBotOwner(Context))
            {
                await Context.ApplyResultReaction(CommandResult.FailedUserPermission).ConfigureAwait(false);
                return;
            }

            // Only list events for the current guild
            if (channel != null && channel.Guild != Context.Guild)
            {
                await Context.ApplyResultReaction(CommandResult.FailedUserPermission).ConfigureAwait(false);
                return;
            }

            var events = new List<Event>();
            await Ditto.Database.DoAsync(uow =>
            {
                events.AddRange(uow.Events.GetAll(e => e.GuildId == Context.Guild.Id));
            }).ConfigureAwait(false);

            // Remove everything that is not the targeted user and/or channel when applicable.
            if (user != null)
            {
                events.RemoveAll(e => e.CreatorId != user.Id);
            }

            if(channel != null)
            {
                events.RemoveAll(e => e.ChannelId != channel.Id);
            }

            // Create the paged embed.
            var embedBuilder = new EmbedBuilder()
                .WithAuthor($"Events{(channel == null ? "" : $" in #{channel.Name}")}{(user == null ? "" : $" from user {user.Username}")}")
                .WithOkColour(Context.Guild)
            ;

            var pagedResults = events.ChunkBy(10);
            embedBuilder.WithFields(GetPageResult(0, pagedResults));

            await Context.SendPagedMessageAsync(embedBuilder, (msg, page) =>
                {
                    embedBuilder.Fields.Clear();
                    embedBuilder.WithFields(GetPageResult(page-1, pagedResults));
                    return embedBuilder;
                },
                pagedResults.Count(),
                true,
                (int)TimeSpan.FromMinutes(5).TotalMilliseconds,
                (int)TimeSpan.FromMinutes(1).TotalMilliseconds
            ).ConfigureAwait(false);
        }

        [DiscordCommand(CommandSourceLevel.Guild, CommandAccessLevel.LocalAndParents)]
        [Alias("remove", "rem", "del")]
        public async Task Delete(int id)
        {
            if (!Permissions.IsAdministratorOrBotOwner(Context))
            {
                await Context.ApplyResultReaction(CommandResult.FailedUserPermission).ConfigureAwait(false);
                return;
            }

            Event @event = null;
            await Ditto.Database.ReadAsync(uow =>
            {
                @event = uow.Events.Get(id);
            }).ConfigureAwait(false);

            if(@event == null)
            {
                await Context.ApplyResultReaction(CommandResult.Failed).ConfigureAwait(false);
            }
            else
            {
                try
                {
                    await Ditto.Database.WriteAsync(uow =>
                    {
                        uow.Events.Remove(@event);
                    }, true).ConfigureAwait(false);
                    await Context.ApplyResultReaction(CommandResult.Success).ConfigureAwait(false);
                }
                catch
                {
                    await Context.ApplyResultReaction(CommandResult.Failed).ConfigureAwait(false);
                }
            }
        }

        private IEnumerable<EmbedFieldBuilder> GetPageResult(int pageIndex, IEnumerable<IEnumerable<Event>> events)
        {
            if (events.Count() >= pageIndex)
            {
                var eventsPage = events.ElementAt(pageIndex);
                foreach (var @event in eventsPage)
                {
                    yield return new EmbedFieldBuilder()
                        .WithName($@"`{@event.Id}`. {@event.Title}")
                        .WithValue(
                            $"*{@event.TimeBegin:hh\\:mm}{(@event.TimeEnd == null ? "" : $"~{@event.TimeEnd.Value:hh\\:mm}")}"
                          + $"{(@event.TimeOffset == null ? "" : $"+{@event.TimeOffset.Value:hh\\:mm}")}"
                          + $" | {@event.CreatorGuild?.Username ?? @event.CreatorGuild?.Username ?? @event.Creator.Username}*"
                        )
                        .WithIsInline(false)
                    ;
                }
            }
        }
    }
}
