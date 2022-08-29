using Discord;
using Discord.Commands;
using Ditto.Attributes;
using Ditto.Bot.Database.Models;
using Ditto.Bot.Modules.Utility.Data;
using Ditto.Data;
using Ditto.Data.Commands;
using Ditto.Data.Discord;
using Ditto.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ditto.Bot.Modules.Utility
{
    public class RemindUtility : DiscordModule<Utility>
    {
        private static ConcurrentDictionary<int, Reminder> _reminders = new ConcurrentDictionary<int, Reminder>();
        private static CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        public static TimeSpan Delay { get; private set; } = TimeSpan.FromMilliseconds(500);
        
        static RemindUtility()
        {
            Ditto.Exit += () =>
            {
                _cancellationTokenSource?.Cancel();
                _reminders.Clear();
                return Task.CompletedTask;
            };

            Ditto.Connected += () =>
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource = new CancellationTokenSource();
                var initialTime = DateTime.Now;

                // Load intiial reminders to _reminders
                IEnumerable<Reminder> remindersDb = null;
                Ditto.Database.Read((uow) =>
                {
                    remindersDb = uow.Reminders.GetAll();
                });
                foreach (var reminder in remindersDb)
                {
                    _reminders.TryAdd(reminder.Id, reminder);
                }

                // Start reading reminders
                try
                {
                    Task.Run(async () =>
                    {
                        while (Ditto.Running)
                        {
                            try
                            {
                                foreach (var reminderPair in _reminders.ToList())
                                {
                                    // check our reminder
                                    var reminder = reminderPair.Value;
                                    if (DateTime.Now > reminder.EndTime)
                                    {
                                        // Verify that the reminder has not already been removed
                                        Reminder reminderDb = null;
                                        await Ditto.Database.ReadAsync((uow) =>
                                        {
                                            reminderDb = uow.Reminders.Get(reminderPair.Key);
                                        });

                                        // If the value is no longer in our database, remove it from our local records.
                                        if (reminderDb == null)
                                        {
                                            _reminders.TryRemove(reminderPair.Key, out Reminder value);
                                            continue;
                                        }
                                        // if for some reason the database version differs, update our local value.
                                        else if (!reminderDb.Equals(reminder))
                                        {
                                            _reminders.TryUpdate(reminderPair.Key, reminderDb, reminder);
                                            continue;
                                        }

                                        // Execute
                                        try
                                        {
                                            var dmChannels = new List<IDMChannel>();
                                            if (reminder.RoleId != null)
                                            {
                                                dmChannels.AddRange(
                                                    (await reminder.Guild.GetUsersAsync())
                                                    .Where(r => r.RoleIds.ToList().Contains(reminder.RoleId.Value))
                                                    .Where(u => !u.IsBot)
                                                    .Select(async u => await u.CreateDMChannelAsync())
                                                    .Select(t => t.Result)
                                                );
                                            }
                                            else if (reminder.ChannelId != null && reminder.Channel is IVoiceChannel)
                                            {
                                                // target everyone in a specific voice channel
                                                dmChannels.AddRange(
                                                        (await (reminder.Channel as IVoiceChannel).GetUsersAsync().ToListAsync()).SelectMany(e => e)
                                                        .Where(u => !u.IsBot)
                                                        .Select(async u => await u.CreateDMChannelAsync()).Select(t => t.Result)
                                                    );
                                            }
                                            else if (reminder.UserId != null)
                                            {
                                                dmChannels.Add(await reminder.User?.CreateDMChannelAsync());
                                            }

                                            var embedBuilder = reminder.Message.Contains("\n") ?
                                            new EmbedBuilder()
                                                    .WithColor(250, 166, 26) // default mention colour
                                                    .WithTitle("\\❗\\⏰ Reminder \\⏰\\❗")
                                                    .WithDescription($"**{reminder.Message}**")
                                                    .WithFooter(new EmbedFooterBuilder()
                                                        .WithText(reminder.Self ? "" : $"sent by {reminder.Creator}")
                                                    )
                                                : null;

                                            var textMessage = $"\\❗⏰ Hey ${{USER}}, {(reminder.Self ? "it's time" : $"I was told to remind you by **{reminder.Creator}**")} to **{reminder.Message}** ⏰\\❗";
                                            if (dmChannels.Count > 0)
                                            {
                                                foreach (var dmChannel in dmChannels)
                                                {
                                                    await (embedBuilder != null ?
                                                        dmChannel.EmbedAsync(embedBuilder, $"Hey {dmChannel.Recipient.Mention}")
                                                        : dmChannel.SendMessageAsync(textMessage.Replace("${USER}", dmChannel.Recipient.Mention))
                                                    );
                                                }
                                            }
                                            else
                                            {
                                                // In case we just don't have any targets
                                                if (reminder.Channel is IMessageChannel channel)
                                                {
                                                    await (embedBuilder != null ?
                                                        channel.EmbedAsync(embedBuilder, $"Hey {channel.Mention()}")
                                                        : channel.SendMessageAsync(textMessage.Replace("${USER}", channel.Mention()))
                                                    );
                                                }
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Log.Error(ex);
                                        }

                                        if (reminder.Repeat)
                                        {
                                            // Update
                                            var timeDifference = reminderDb.EndTime - reminderDb.StartTime;
                                            var startTime = DateTime.Now;
                                            var endTime = startTime + timeDifference;
                                            
                                            await Ditto.Database.WriteAsync((uow) =>
                                            {
                                                reminderDb = uow.Reminders.Get(reminderPair.Key);
                                                if (reminderDb != null)
                                                {
                                                    reminderDb.StartTime = startTime;
                                                    reminderDb.EndTime = endTime;
                                                }
                                            }).ConfigureAwait(false);

                                            if (_reminders.TryGetValue(reminderPair.Key, out Reminder value))
                                            {
                                                value.StartTime = startTime;
                                                value.EndTime = endTime;
                                            }
                                        }
                                        else
                                        {
                                            // Remove
                                            await Ditto.Database.WriteAsync((uow) =>
                                            {
                                                uow.Reminders.Remove(reminderPair.Key);
                                            });
                                            _reminders.TryRemove(reminderPair.Key, out Reminder value);
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex);
                            }
                            await Task.Delay(Delay);
                        }
                    }, _cancellationTokenSource.Token);
                }
                catch (TaskCanceledException) { }
                catch (OperationCanceledException) { }

                Log.Info("Started after {0}ms", (int)(DateTime.Now - initialTime).TotalMilliseconds);
                return Task.CompletedTask;
            };
        }
        
        public static ReminderParseResult ParseReminder(string message)
        {
            TimeSpan? time = null;
            string timeString = null;
            string text = null;
            string error = null;
            bool repeat = false;

            var regex = Globals.RegularExpression.RemindMessage;
            var match = regex.Match(message);

            // Get our time values
            foreach(var groupName in regex.GetGroupNames())
            {
                if (groupName == "0")
                    continue;
                var groupValue = match.Groups[groupName].Value;
                if (groupValue.Length == 0)
                    continue;

                if (groupName == "text")
                {
                    // Do some markdown checks
                    text = groupValue
                        .Replace("`Markdown", "")
                        .Replace("`", "");

                    // Multiline
                    if(text.Contains("\n"))
                    {
                        text = $"```Markdown\n{text}```";
                    }
                    else
                    {
                        text = $" `{text}` ";
                    }
                }
                else if(groupName == "repeat")
                {
                    repeat = true;
                }
                else
                {
                    if(int.TryParse(groupValue, out int value))
                    {
                        time = (time ?? (time = new TimeSpan())).Value.Add(new TimeSpan(
                            groupName == "months" ? 30 * value :
                            groupName == "weeks" ? 7 * value :
                            groupName == "days" ? value : 0
                            , groupName == "hours" ? value : 0
                            , groupName == "minutes" ? value : 0
                            , groupName == "seconds" ? value : 0
                        ));
                        if (timeString == null)
                            timeString = "";

                        if (timeString.Length > 0)
                            timeString += ", ";

                        timeString += $"{value} {groupName}";
                    }
                    else
                    {
                        // Parse error
                        Log.Warn($"Invalid {groupName} value");
                        if(error == null)
                            error = $"Invalid {groupName}";
                        else
                            error += $"{groupName.ToTitleCase()}, ";
                    }
                }
            }

            // If either we provided no time at all or the provided time is too short.
            if((!time.HasValue && error == null))
            {
                error = "Please enter a valid time.";
            }
            else if(time.HasValue && time.Value.TotalSeconds < 10)
            {
                error = "Please enter a time that is greater than or equal to 10 seconds.";
            }

            if(error != null && !error.EndsWith("."))
                error += ".";

            return new ReminderParseResult()
            {
                Text = text,
                Time = time,
                Repeat = repeat,
                Error = error,
                TimeString = timeString
            };
        }

        private async Task Remind(ISnowflakeEntity snowflakeEntity, [Multiword] string message)
        {
            var user = snowflakeEntity as IUser;
            var role = snowflakeEntity as IRole;
            var channel = snowflakeEntity as ITextChannel;
            var voiceChannel = snowflakeEntity as IVoiceChannel;
            
            if (snowflakeEntity == null && user == null && role == null && channel == null && voiceChannel == null)
            {
                await Context.EmbedAsync(
                    "Incorrect parameter",
                    ContextMessageOption.ReplyWithError
                ).ConfigureAwait(false);
                return;
            }

            var result = ParseReminder(message);
            if (result.Success)
            {
                var dateNow = DateTime.Now;
                var dateStart = dateNow + result.Time.Value;

                Reminder reminder = null;
                await Ditto.Database.WriteAsync((uow) =>
                {
                    reminder = uow.Reminders.Add(new Reminder()
                    {
                        Guild = Context.Guild,
                        Channel = (IChannel)channel ?? voiceChannel,
                        User = user,
                        Role = role,
                        Creator = Context.NicknameAndGlobalUsername,
                        StartTime = dateNow,
                        EndTime = dateNow + (result.Time.Value),
                        Message = result.Text,
                        Repeat = result.Repeat,
                        Self = (Context.User.Id == user?.Id),
                    });
                });
                if (reminder == null)
                {
                    // Unexpected error?
                    await Context.EmbedAsync(
                        "Unexpected error occured, please try again.",
                        ContextMessageOption.ReplyWithError
                    ).ConfigureAwait(false);
                }
                else
                {
                    _reminders.TryAdd(reminder.Id, reminder);

                    await Context.EmbedAsync(new EmbedBuilder()
                        .WithDescription($"\\⏰ I will remind **{user?.Mention ?? role?.Mention ?? channel?.Mention() ?? voiceChannel?.Mention()}** to **{result.Text + (result.Text.Contains("\n") ? "\n" : "") ?? " "}** {(result.Repeat ? "every" : "in")} **{result.TimeString}**")
                        .WithFooter(new EmbedFooterBuilder()
                            .WithText($"executing on {dateStart:dd-MM-yyyy} at {dateStart:HH:mm:ss}")
                        )
                    );
                }
            }
            else
            {
                await Context.ApplyResultReaction(CommandResult.Failed).ConfigureAwait(false);
                await Context.EmbedAsync(
                    result.Error,
                    ContextMessageOption.ReplyWithError
                ).ConfigureAwait(false);
            }
        }

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.Global | CommandAccessLevel.Local)]
        [Priority(1)]
        public Task Remind(IUser user, [Multiword] string message)
            => Remind((ISnowflakeEntity)user, message);

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.Global | CommandAccessLevel.Local)]
        [Priority(2)]
        public Task Remind(IRole role, [Multiword] string message)
            => Remind((ISnowflakeEntity)role, message);

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.Global | CommandAccessLevel.Local)]
        [Priority(3)]
        public async Task Remind(ITextChannel channel, [Multiword] string message)
        {
            // Only allow using channels of the current guild.
            if (channel != null && channel.Guild != Context.Guild)
            {
                await Context.ApplyResultReaction(CommandResult.FailedUserPermission).ConfigureAwait(false);
                return;
            }

            await Remind((ISnowflakeEntity)channel, message).ConfigureAwait(false);
        }
        
        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.Global | CommandAccessLevel.Local)]
        [Priority(4)]
        public Task Remind(IVoiceChannel channel, [Multiword] string message)
            => Remind((ISnowflakeEntity)channel, message);

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.Global | CommandAccessLevel.Local)]
        [Alias("remindme")]
        [Priority(5)]
        [Description("Reminds the target (user, role, channel) of a message in a specified time, time can be formatted in many ways but must follow the unit order, weeks>days>minutes..."
            + "\nFor repeatable reminders the user must add \"repeat\" or \"every\" at the start of the message."
            + "\nExamples: " + "* remind me every 10 minutes and 30sec to check my emails"
            + "\n* remind @Kaa#2195 1wm2d Holliday plans"
        )]
        public Task Remind(string who, [Multiword] string message)
        {
            who = who.ToLower().Trim();
            if(who == "me")
            {
                // remind current user
                return Remind(Context.User, message);
            }
            else if(who == "here")
            {
                // remind current channel
                return Remind(Context.Channel, message);
            }
            else
            {   
                // default, assume user reminder
                return Remind(Context.User, who + " " + message);
            }
        }
    }
}
