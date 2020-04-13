using Discord;
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
    [Alias("event")]
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
                        const int timeTolerance = 5;
                        var dateNow = DateTime.UtcNow;
                        foreach (var pair in _events.ToList())
                        {
                            var @event = pair.Value;
                            // Reversed offset due to it noting the timezone
                            var timeOffset = @event.TimeOffset ?? TimeSpan.FromSeconds(0);
                            var timeBegin = @event.TimeBegin - timeOffset;
                            var timeEnd = (@event.TimeEnd ?? @event.TimeBegin) - timeOffset;

                            if ((dateNow - (@event.LastRun ?? DateTime.MinValue)).TotalHours >= 1
                               && DateHelper.HasDayOfWeek(@event.Days, dateNow.DayOfWeek)
                               && Math.Abs((dateNow.TimeOfDay - timeBegin).TotalMinutes) <= timeTolerance
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

                                        if (null != await textChannel.EmbedAsync(@event.MessageHeader, embedBuilder).ConfigureAwait(false))
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
        public override Task _()
        {
            return Task.CompletedTask;
        }

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.LocalAndParents)]
        [Help(null, "Create an event that will trigger based on the specified day(s) and time.")]
        public async Task Add(
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
                    Channel = Context.Channel,
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

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.Global | CommandAccessLevel.Local)]
        public Task Add(string timeMessage, [Optional] string title, [Optional] string header, [Multiword] string message)
            => Add(EventDay.Daily, timeMessage, title, header, message);
    }
}
