using Discord;
using Discord.Commands;
using Ditto.Attributes;
using Ditto.Bot.Database.Data;
using Ditto.Bot.Database.Models;
using Ditto.Bot.Modules.BDO.Data;
using Ditto.Bot.Modules.Utility.Linking;
using Ditto.Data.Commands;
using Ditto.Data.Discord;
using Ditto.Extensions;
using Ditto.Extensions.Discord;
using Ditto.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Ditto.Bot.Modules.BDO
{
    public class BDO : DiscordModule
    {
        public static bool Initialized { get; private set; } = false;
        public static bool Running { get; private set; } = false;
        public static TimeSpan Delay { get; private set; } = new TimeSpan();
        public static BDOClock Clock { get; private set; } = new BDOClock();
        public static BdoStatusResult ServerStatus { get; private set; } = BdoStatusResult.InvalidResult;

        private static string _loginUrl = "";
        private static string _loginTokenUrl = "";
        private static string _launcherUrl = "";
        private static DateTime _lastServerStatusTime = DateTime.MinValue;
        private static ConcurrentDictionary<ulong, ITextChannel> _bdoChannels;
        private static CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        /// Invoked when the server status changes, the first BdoStatusResult is the new status, the second is the old one.
        /// </summary>
        public static Func<BdoStatusResult, BdoStatusResult,Task> ServerStatusChanged;

        static BDO()
        {
            Ditto.Connected += async () =>
            {
                if (string.IsNullOrWhiteSpace(Ditto.Settings.BlackDesertOnline.Email)
                || string.IsNullOrWhiteSpace(Ditto.Settings.BlackDesertOnline.Password)
                || string.IsNullOrWhiteSpace(Ditto.Settings.BlackDesertOnline.LoginUrl))
                {
                    Log.Warn("Ignored the module Black Desert Online");
                    return;
                }
                
                // Properties & Fields
                Delay = TimeSpan.FromMinutes(1);
                _launcherUrl = Ditto.Settings.BlackDesertOnline.LauncherUrl;
                _loginUrl = string.Format(
                    Ditto.Settings.BlackDesertOnline.LoginUrl,
                    Ditto.Settings.BlackDesertOnline.Email,
                    Ditto.Settings.BlackDesertOnline.Password
                );
                _loginTokenUrl = Ditto.Settings.BlackDesertOnline.LoginTokenUrl;

                _bdoChannels = new ConcurrentDictionary<ulong, ITextChannel>(
                        await Ditto.Client.DoAsync((client) => client.Guilds.ToDictionary(g => g.Id,
                            (gc) =>
                            {
                                string channelIdString = null;
                                Ditto.Database.Read((uow) =>
                                {
                                    channelIdString = uow.Configs.GetBdoMaintenanceChannel(gc as IGuild)?.Value;
                                });
                                if(ulong.TryParse(channelIdString, out ulong channelId))
                                {
                                    return (ITextChannel)gc.GetTextChannel(channelId);
                                }
                                return null;
                            }
                        ))
                );

                // Load default server status
                BdoStatus bdoStatus = null;
                await Ditto.Database.ReadAsync(uow =>
                {
                    bdoStatus = uow.BdoStatus.GetAll().FirstOrDefault();
                }).ConfigureAwait(false);

                if(bdoStatus == null)
                {
                    var serverStatus = await GetServerStatusAsync().ConfigureAwait(false);
                    await Ditto.Database.WriteAsync(uow =>
                    {
                        bdoStatus = uow.BdoStatus.Add(new BdoStatus()
                        {
                            Status = serverStatus.Status,
                            MaintenanceTime = serverStatus.MaintenanceTime,
                            Error = serverStatus.Error,
                            DateUpdated = DateTime.Now
                        });
                    }).ConfigureAwait(false);
                }
                _lastServerStatusTime = bdoStatus.DateUpdated;
                ServerStatus = new BdoStatusResult()
                {
                    Status = bdoStatus.Status,
                    MaintenanceTime = bdoStatus.MaintenanceTime,
                    Error = bdoStatus.Error
                };

                ServerStatusChanged += (@new, old) =>
                {
                    // Update database
                    var changed = @new != old;
                    var dbTask = Task.Run(async () =>
                    {

                        BdoStatus dbStatus = null;
                        await Ditto.Database.ReadAsync(uow =>
                            dbStatus = uow.BdoStatus.GetAll().FirstOrDefault()
                        ).ConfigureAwait(false);

                        if (dbStatus == null)
                        {
                            dbStatus = await Ditto.Database.WriteAsync(uow =>
                                uow.BdoStatus.Add(new BdoStatus()
                                {
                                    Status = @new.Status,
                                    Error = @new.Error,
                                    MaintenanceTime = @new.MaintenanceTime,
                                    DateUpdated = DateTime.Now
                                })
                            ).ConfigureAwait(false);
                        }
                        else if (changed)
                        {
                            bdoStatus.Status = @new.Status;
                            bdoStatus.Error = @new.Error;
                            bdoStatus.MaintenanceTime = @new.MaintenanceTime;
                            bdoStatus.DateUpdated = DateTime.Now;

                            await Ditto.Database.WriteAsync(uow =>
                            {
                                uow.BdoStatus.Update(bdoStatus);
                            });
                        }
                    });
                    return Task.CompletedTask;
                };

                // Automatically update the server status
                Running = true;
                var _ = Task.Run(async () =>
                {
                    await Task.Delay(1000).ConfigureAwait(false);
                    while (Running)
                    {
                        // Update status
                        await GetServerStatusAsync().ConfigureAwait(false);
                        await Task.Delay(TimeSpan.FromMinutes(1), _cancellationTokenSource.Token).ConfigureAwait(false);
                    }
                }, _cancellationTokenSource.Token);


                // Post channel updates
                //ServerStatusChanged += async (status, old) =>
                //{
                //    foreach (var channel in BdoMaintenanceLinking.GetLinkedChannels())
                //    {
                //        // TODO
                //    }
                //};

                Initialized = true;
            };

            Ditto.Exit += () =>
            {
                Running = false;
                _cancellationTokenSource?.Cancel();
                return Task.CompletedTask;
            };
        }
        
        public static async Task<BdoStatusResult> GetServerStatusAsync()
        {
            if ((DateTime.Now - _lastServerStatusTime) < Delay)
            {
                return ServerStatus;
            }

            var statusResult = BdoStatusResult.InvalidResult;
            using (var client = new HttpClient())
            {
                var loginDataJson = await client.GetStringAsync(_loginUrl).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(loginDataJson))
                {
                    dynamic results = JsonConvert.DeserializeObject<dynamic>(loginDataJson);
                    var errorCode = results?.api?.additionalInfo?.code;
                    string token = results?.result?.token;
                    string errorMessage = results?.api?.additionalInfo?.msg ?? (results?.api?.codeMsg ?? "" + $"({results?.api?.additionalInfo.name})");
                    
                    // Logged in
                    if (errorCode == null)
                    {
                        // Try and create the "play token"
                        var playTokenDataJson = await client.GetStringAsync(string.Format(_loginTokenUrl, token)).ConfigureAwait(false);
                        if (!string.IsNullOrEmpty(playTokenDataJson))
                        {
                            results = JsonConvert.DeserializeObject<dynamic>(playTokenDataJson);
                            errorCode = results?.api?.additionalInfo?.code;
                            errorMessage = results?.api?.additionalInfo?.msg ?? (results?.api?.codeMsg ?? "" + $"({results?.api?.additionalInfo.name})");
                            if (errorCode == null)
                            {
                                statusResult = new BdoStatusResult()
                                {
                                    Status = BdoServerStatus.Online
                                };
                            }
                            else if (errorCode >= 412 && errorCode <= 415)
                            {
                                // Read maintenance time
                                var launcherHtmlCode = await client.GetStringAsync(_launcherUrl).ConfigureAwait(false);
                                var maintenance = launcherHtmlCode.Between("'error_maintenance':\"", "\",").Replace("\\n", "\n");
                                if (maintenance != null)
                                {
                                    //var maintenanceTimeString = maintenance.Between("~", ")")
                                    //    .Replace("UTC", "")
                                    //    .Trim();
                                    //var maintenanceTimeString = maintenance.From("time:").Between(" to ", "UTC").Trim();
                                    var maintenanceTimeString = Regex.Match(maintenance, @"(?:(?<time>\d+:\d+) UTC)").Groups?.Values?.LastOrDefault()?.Value;
                                    DateTime? maintenanceDateTime = null;
                                    if (TimeSpan.TryParse(maintenanceTimeString, out TimeSpan maintenanceTime))
                                    {
                                        var utcDate = DateTime.UtcNow;
                                        maintenanceDateTime = ((utcDate - utcDate.TimeOfDay) + maintenanceTime);
                                    }
                                    statusResult = new BdoStatusResult()
                                    {
                                        Status = BdoServerStatus.Maintenance,
                                        Error = maintenance,
                                        MaintenanceTime = maintenanceDateTime?.ToLocalTime()
                                    };
                                }
                            }
                        }
                    }
                    
                    // Unknown error
                    if (statusResult.Status == BdoServerStatus.Unknown)
                    {
                        statusResult = new BdoStatusResult()
                        {
                            Status = BdoServerStatus.Unknown,
                            Error = errorMessage
                        };
                    }
                }
            }

            // Modify value if changed and call the event handler if needed
            if (ServerStatus != statusResult)
            {
                if(_lastServerStatusTime != DateTime.MinValue)
                {
                    //var _ = Task.Run(async ()
                    //    => await ServerStatusChanged(statusResult, ServerStatus).ConfigureAwait(false)
                    //);
                    await ServerStatusChanged(statusResult, ServerStatus).ConfigureAwait(false);
                }
                ServerStatus = statusResult;
            }
            _lastServerStatusTime = DateTime.Now;
            return statusResult;
        }

        [DiscordCommand(CommandSourceLevel.Guild, CommandAccessLevel.LocalAndParents, DeleteUserMessage = true)]
        public async Task Status(ITextChannel textChannel)
        {
            var success = false;
            var channel = (textChannel ?? Context.Channel as ITextChannel);
            if (channel != null)
            {
                if (!(await Ditto.Client.DoAsync(async c => await c.GetPermissionsAsync(textChannel)).ConfigureAwait(false)).HasAccess())
                {
                    await Context.Channel.SendMessageAsync($"💢 {Context.User.Mention} Unable to access the channel {channel.Mention}");
                }
                else
                {
                    success = await LinkUtility.TryAddLinkAsync(LinkType.BDO_Maintenance, channel, null).ConfigureAwait(false);
                    await Context.EmbedAsync(
                        success
                        ? $"Successfully linked BDO maintenance to {textChannel.Mention}"
                        : $"",
                        success ? ContextMessageOption.ReplyUser : ContextMessageOption.ReplyWithError,
                        new RequestOptions() { RetryMode = RetryMode.AlwaysRetry }
                    ).DeleteAfterAsync(10).ConfigureAwait(false);
                }
            }
            else
            {

            }
        }

        public static EmbedBuilder GetStatusEmbedBuilder()
        {
            return new EmbedBuilder()
                .WithAuthor(
                    $"Black Desert Online - Maintenance",
                    @"https://akamai-webcdn.blackdesertonline.com/bdo/web/images/logo.og.png"
                )
                .WithDescription(
                    "```" +
                    $"Login: {(ServerStatus.Status == BdoServerStatus.Online ? "Online ✅" : "Offline ⛔")}\n\n" +
                    $"{(string.IsNullOrWhiteSpace(ServerStatus.Error) ? "" : ServerStatus.Error)}" +
                    "```"
                )
                .WithFooter(
                    $"⏰ ending at {ServerStatus.MaintenanceTime:dd-MM-yyyy}" +
                    $" at {ServerStatus.MaintenanceTime:HH:mm}"
                );
        }

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.LocalAndParents, DeleteUserMessage = true)]
        public async Task Status()
        {
            await Context.EmbedAsync(GetStatusEmbedBuilder(), ContextMessageOption.None, RetryMode.AlwaysRetry);
        }

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.All, DeleteUserMessage = true)]
        public Task BdoStatus()
        {
            return Status();
        }
    }
}
