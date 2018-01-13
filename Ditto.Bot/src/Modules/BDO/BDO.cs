using Discord;
using Ditto.Attributes;
using Ditto.Bot.Database.Data;
using Ditto.Bot.Database.Models;
using Ditto.Bot.Modules.BDO.Data;
using Ditto.Data.Discord;
using Ditto.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Ditto.Bot.Modules.BDO
{
    public class BDO : DiscordModule
    {
        public static bool Initialized { get; private set; } = false;
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

                ServerStatusChanged += async (@new, old) =>
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
                };

                var _ = Task.Run(async () =>
                {
                    await Task.Delay(1000).ConfigureAwait(false);
                    while (Ditto.Running)
                    {
                        // Update status
                        await GetServerStatusAsync().ConfigureAwait(false);
                        await Task.Delay(TimeSpan.FromMinutes(1), _cancellationTokenSource.Token).ConfigureAwait(false);
                    }
                }, _cancellationTokenSource.Token);
                Initialized = true;
            };

            Ditto.Exit += () =>
            {
                _cancellationTokenSource.Cancel();
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
                                var maintenance = launcherHtmlCode.Between("'error_maintenance':\"", "\",");
                                if (maintenance != null)
                                {
                                    var maintenanceTimeString = maintenance.Between("~", ")")
                                        .Replace("UTC", "")
                                        .Trim();
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
                    var _ = Task.Run(async ()
                        => await ServerStatusChanged(statusResult, ServerStatus).ConfigureAwait(false)
                    );
                }
                ServerStatus = statusResult;
            }
            _lastServerStatusTime = DateTime.Now;
            return statusResult;
        }
    }
}
