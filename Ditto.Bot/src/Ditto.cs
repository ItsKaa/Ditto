using Discord;
using Discord.WebSocket;
using Ditto.Bot.Data;
using Ditto.Bot.Data.Configuration;
using Ditto.Bot.Database.Data;
using Ditto.Bot.Database.Models;
using Ditto.Bot.Modules.BDO;
using Ditto.Bot.Services;
using Ditto.Bot.Services.Data;
using Ditto.Data;
using Ditto.Data.Discord;
using Ditto.Data.Exceptions;
using Ditto.Extensions;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ditto.Bot
{
    public enum BotType
    {
        Bot = 0,
        User = 1
    }

    public class Ditto
    {
        public static ObjectLock<DiscordClientEx> Client { get; private set; }
        public static DatabaseHandler Database { get; private set; }
        public static CommandHandler CommandHandler { get; private set; }
        public static BotType Type { get; private set; } = BotType.Bot;
        public static bool Running { get; private set; } = false;
        public static CacheHandler Cache { get; private set; }
        public static GoogleService Google { get; private set; }
        public static Giphy Giphy { get; private set; }
        public static SettingsConfiguration Settings { get; private set; }
        public static ReactionHandler ReactionHandler { get; private set; }

        /// <summary>Fired when the bot connects for the first time.</summary>
        public static Func<Task> Connected;

        /// <summary>Fired when the bot shuts down.</summary>
        public static Func<Task> Exit;

        private static bool _exiting = false;
        private static bool _reconnecting = false;
        private static bool _firstStart = true;
        
        static Ditto()
        {
            Program.Exit += async () =>
            {
                _exiting = true;
                Log.Warn("Shutting down...");
                await Task.Delay(100);
                
                if (Exit != null)
                {
                    try { await Exit().ConfigureAwait(false); } catch { }
                }

                await Task.Delay(1000);

                if (Client != null && Client.HasValue)
                {
                    await LogOutAsync().ConfigureAwait(false);
                }
                await Task.Delay(1000);

                Program.Close();
                Log.Info("OK.");
                await Task.Delay(1000);
            };
        }

        private static async Task LogOutAsync() => await Client.DoAsync((c) =>
        {
            try
            {
                return LogOutAsync(c);
            }
            catch { }
            return Task.CompletedTask;
        });

        public static bool IsClientConnected(DiscordClientEx client)
        {
            if (client != null)
            {
                return (client != null
                    && Running
                    && client.ConnectionState == ConnectionState.Connected
                    && client.LoginState == LoginState.LoggedIn
                );
            }
            return false;
        }
        public static async Task<bool> IsClientConnectedAsync()
            => await Client.DoAsync(client => IsClientConnected(client));
            
        public static bool IsClientConnected()
        {
            if(Client.HasValue)
            {
                return Client.Do((client) =>
                {
                    return (client != null
                        && Running
                        && client.ConnectionState == ConnectionState.Connected
                        && client.LoginState == LoginState.LoggedIn
                    );
                });
            }
            return false;
        }


        private static async Task LogOutAsync(DiscordClientEx client)
        {
            if (!Running)
                return;
            Running = false;

            try { await client.SetGameAsync(null); } catch { }
            try { await client.SetStatusAsync(UserStatusEx.Invisible); } catch { }
            try { await client.LogoutAsync(); } catch { }
        }

        private static async Task LoginAsync()
            => await Client.DoAsync((c) => LoginAsync(c));

        private static async Task LoginAsync(DiscordClientEx client)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            await client.LoginAsync((Type == BotType.Bot ? TokenType.Bot : TokenType.User), Settings.Credentials.BotToken, true).ConfigureAwait(false);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        public async Task ReconnectAsync()
        {
            _reconnecting = true;
            await Task.Delay(1000);
            Log.Info("Reconnecting...");
            try
            {
                try
                {
                    await Client.DoAsync(async (c) =>
                    {
                        await c.StopAsync().ConfigureAwait(false);
                    }).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Log.Warn("Error at Client.StopAsync | {0}", ex);
                }

                try { await Exit().ConfigureAwait(false); } catch(Exception ex) { Log.Warn("Error at Exit | {0}", ex); }
                try { PlayingStatusHandler.Stop(); } catch (Exception ex) { Log.Warn("Error at PlayingStatusHandler.Stop | {0}", ex); }
                try { PlayingStatusHandler.Clear(); } catch (Exception ex) { Log.Warn("Error at PlayingStatusHandler.Clear | {0}", ex); }
                try { CommandHandler.Dispose(); } catch (Exception ex) { Log.Warn("Error at CommandHandler.Dispose | {0}", ex); }
                try { ReactionHandler.Dispose(); } catch (Exception ex) { Log.Warn("Error at ReactionHandler.Dispose | {0}", ex); }
                try { Client.Dispose(); } catch (Exception ex) { Log.Warn("Error at Client.Dispose | {0}", ex); }
                Running = false;

                await RunAsync().ConfigureAwait(false);
                Log.Info("Bot has been reconnected to the server.");
            }
            catch (Exception ex)
            {
                Log.Error("Could not reconnect, retrying in 1 minute", ex);
                await Task.Delay(TimeSpan.FromMinutes(1)).ConfigureAwait(false);
                //await ReconnectAsync().ConfigureAwait(false);
                var _ = Task.Run(() => ReconnectAsync());
            }
            _reconnecting = false;
        }
        

        public async Task RunAsync()
        {
            if (_firstStart)
            {
                Log.Setup(true, false);
                Log.Info("Starting Ditto...");
            }
            
            // Try to load the settings from the XML file.
            // Should this not exist, it will automatically create one.
            try
            {
                Settings = await SettingsConfiguration.ReadAsync("data/settings.xml");
            }catch(Exception ex)
            {
                Log.Fatal(ex);
                return;
            }
            
            // Try to initialize the database
            try
            {
                (Database = new DatabaseHandler()).Setup(Settings.Credentials.Sql.Type, Settings.Credentials.Sql.ConnectionString);
            }
            catch(Exception ex)
            {
                Log.Fatal("Unable to create a connection with the database, please check the file \"/data/settings.xml\"", ex);
                return;
            }
            
            // Try to initialise the service 'Google'
            try
            {
                await (Google = new GoogleService()).SetupAsync(Settings.Credentials.GoogleApiKey).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Warn("Could not initialize Google {0}\n", ex.ToString());
            }

            // Try to initialise 'Giphy'
            try
            {
                Giphy = new Giphy(Settings.Credentials.GiphyApiKey);
            }
            catch (ApiException ex)
            {
                Log.Warn("Could not initialize Giphy {0}\n", ex.ToString());
            }

            // Create our discord client
            Client = new ObjectLock<DiscordClientEx>(new DiscordClientEx(new DiscordSocketConfig()
            {
                MessageCacheSize = Settings.AmountOfCachedMessages,
                LogLevel = LogSeverity.Warning,
                //TotalShards = 1,
                ConnectionTimeout = Settings.TimeoutInMilliseconds,
                HandlerTimeout = Settings.TimeoutInMilliseconds,
                DefaultRetryMode = RetryMode.AlwaysRetry,
                //AlwaysDownloadUsers = true,
            }), 1, 1);
            
            // Various services
            (Cache = new CacheHandler()).Setup(TimeSpan.FromSeconds(Settings.CacheTime));
            await (CommandHandler = new CommandHandler(Client)).SetupAsync().ConfigureAwait(false);


            await Client.DoAsync((client)
                => client.Connected += async () =>
                {
                    // Setup services
                    await CommandHandler.SetupAsync().ConfigureAwait(false);
                    await (ReactionHandler = new ReactionHandler()).SetupAsync(Client).ConfigureAwait(false);
                    PlayingStatusHandler.Setup(TimeSpan.FromMinutes(1));
                }
            ).ConfigureAwait(false);

            if (_firstStart)
            {
                Connected += async () =>
                {
                    // Start services
                    await CommandHandler.StartAsync().ConfigureAwait(false);
                    PlayingStatusHandler.Start();
                };
            }
            
            await Client.DoAsync((client)
                => client.Ready += async () =>
                {
                    if (!Running)
                    {
                        Running = true;
                        await Connected().ConfigureAwait(false);
                    }
                    Log.Info("Connected");
                    
                    if (Type == BotType.Bot)
                    {
                        await client.SetGameAsync(null);
                        await client.SetStatusAsync(UserStatusEx.Online);
                    }
                }
            ).ConfigureAwait(false);
            
            await Client.DoAsync((client)
                => client.Disconnected += (e) =>
                {
                    if (_reconnecting)
                    {
                        //_reconnecting = false;
                    }
                    else
                    {
                        Log.Warn("Bot has been disconnected. {0}", (object)(_exiting ? null : $"| {e}"));
                        if (!_exiting && Settings.AutoReconnect && !_reconnecting)
                        {
                            var _ = Task.Run(() => ReconnectAsync());
                        }
                    }
                    return Task.CompletedTask;
                }
            ).ConfigureAwait(false);
            
            await Client.DoAsync((client)
                => client.LoggedOut += () =>
                {
                    Log.Warn("Bot has logged out.");
                    if (!_exiting && Settings.AutoReconnect && !_reconnecting)
                    {
                        var _ = Task.Run(() => ReconnectAsync());
                    }
                    return Task.CompletedTask;
                }
            ).ConfigureAwait(false);
            

            // Playing status Handler, Black Desert Online
            PlayingStatusHandler.Register(PriorityLevel.Normal, (client) =>
            {
                var serverStatus = BDO.ServerStatus;
                if (BDO.ServerStatus.Status == BdoServerStatus.Online)
                {
                    BDO.Clock.Update();
                    return BDO.Clock.UntilNightOrDayString;
                }
                else if (serverStatus.Status == BdoServerStatus.Maintenance && serverStatus.MaintenanceTime.HasValue)
                {
                    var difference = serverStatus.MaintenanceTime.Value - DateTime.Now;
                    if (serverStatus.MaintenanceTime.Value < DateTime.Now || difference.TotalSeconds <= 60)
                    {
                        return "Server up soon™";
                    }
                    return "Server up in " + difference.Get(TimeUnit.FromMinutes).Humanize(true, "");
                }
                return null;
            });

            BDO.ServerStatusChanged += async (@new, old) =>
            {
                bool found = false;
                bool maintenanceStart = false;


                if (old.Status == BdoServerStatus.Online && @new.Status == BdoServerStatus.Maintenance)
                {
                    // Maintenance started
                    found = true;
                    maintenanceStart = true;
                }
                else if(old.Status == BdoServerStatus.Maintenance && @new.Status == BdoServerStatus.Online)
                {
                    // Maintenance ended
                    found = true;
                    maintenanceStart = false;
                }
                

                // >bdo link "maintenance #general"
                if (found)
                {
                    var links = Enumerable.Empty<Link>();
                    await Database.ReadAsync((uow) =>
                    {
                        links = uow.Links.GetAllWithLinks(i => i.Type == LinkType.BDO_Maintenance);
                    }).ConfigureAwait(false);
                    
                    foreach (var link in links)
                    {
                        if (link.Type == LinkType.BDO_Maintenance)
                        {
                            if (link.Channel is ITextChannel textChannel)
                            {
                                if (maintenanceStart)
                                {
                                    //textChannel.EmbedAsync(new EmbedBuilder())
                                }
                            }
                        }
                    }
                }
            };

            
            var autoReconnect = Settings.AutoReconnect;
            Settings.AutoReconnect = false;
            try
            {
                await LoginAsync().ConfigureAwait(false);
                Settings.AutoReconnect = autoReconnect;
            }
            catch(Exception ex)
            {
                Log.Fatal("Failed to login, please check your internet connection and bot token.", ex);
                return;
            }
            await Client.DoAsync((c) => c.StartAsync()).ConfigureAwait(false);
            _firstStart = false;
        }
        

        public async Task RunAndBlockAsync(CancellationToken token)
        {
            await RunAsync().ConfigureAwait(false);

            // Block this task until the program is closed.
            try
            {
                await Task.Delay(-1, token).ConfigureAwait(false);
            }
            catch { }

            //Log.Info("Thread has been cancelled.");
            await Task.Delay(1000);
            Environment.Exit(0);
        }
    }

}