﻿using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Ditto.Bot.Data.API;
using Ditto.Bot.Data.Configuration;
using Ditto.Bot.Services;
using Ditto.Bot.Services.Commands;
using Ditto.Extensions;
using Ditto.Data.Discord;
using Ditto.Data.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using System;
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
        public static BotType Type { get; private set; } = BotType.Bot;
        public static bool Running { get; private set; } = false;
        private static bool Stopping { get; set; } = false;
        public static bool Reconnecting { get; private set; } = false;
        public static bool Exiting { get; private set; } = false;
        public static DiscordSocketClient Client { get; private set; }
        public static DatabaseHandler Database { get; private set; }
        public static CommandHandler CommandHandler { get; private set; }
        public static InteractionService InteractionService { get; private set;  }
        public static IServiceProvider ServiceProvider { get; private set; }

        public static CacheHandler Cache { get; private set; }
        public static GoogleService Google { get; private set; }
        public static TwitchLib.Api.TwitchAPI Twitch { get; private set; }
        public static Giphy Giphy { get; private set; }
        public static SettingsConfiguration Settings { get; private set; }
        public static ReactionHandler ReactionHandler { get; private set; }
        
        private static bool _firstStart = true;

        /// <summary>Fired at the first successful connection, this will only be called once.</summary>
        public static Func<Task> Initialised;

        /// <summary>Fired when the bot connects for the first time.</summary>
        public static Func<Task> Connected;

        /// <summary>Fired when the bot shuts down.</summary>
        public static Func<Task> Exit;

        
        static Ditto()
        {
            Program.Exit += async () =>
            {
                Exiting = true;
                Log.Warn("Shutting down...");
                await Task.Delay(100);
                
                if (Exit != null)
                {
                    try { await Exit().ConfigureAwait(false); } catch { }
                }

                await Task.Delay(1000);

                if (Client != null)
                {
                    await LogOutAsync().ConfigureAwait(false);
                }
                await Task.Delay(1000);

                Program.Close();
                Log.Info("OK.");
                await Task.Delay(1000);
            };
        }

        public static async Task StopAsync()
        {
            Stopping = true;
            if (Exit != null)
            {
                try { await Exit().ConfigureAwait(false); } catch { }
            }
            if (Client != null)
            {
                await LogOutAsync().ConfigureAwait(false);
            }

            Client?.Dispose();
            ReactionHandler?.Dispose();
            Database = null;
            Cache = null;
        }

        private static async Task LogOutAsync()
        {
            try
            {
                await LogOutAsync(Client);
            }
            catch { }
            return;
        }

        public static bool IsClientConnected(DiscordSocketClient client)
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
            
        public static bool IsClientConnected()
        {
            return Client != null
                && Running
                && Client.ConnectionState == ConnectionState.Connected
                && Client.LoginState == LoginState.LoggedIn;
        }


        private static async Task LogOutAsync(DiscordSocketClient client)
        {
            if (!Running)
                return;
            Running = false;

            try { await client.SetGameAsync(null); } catch { }
            try { await client.SetStatusExAsync(UserStatusEx.Invisible); } catch { }
            try { await client.LogoutAsync(); } catch { }
        }

        private static Task LoginAsync() => LoginAsync(Client);

        private static Task LoginAsync(DiscordSocketClient client)
        {
            return client?.LoginAsync(Type == BotType.Bot ? TokenType.Bot : 0, Settings.Credentials.BotToken, true) ?? Task.CompletedTask;
        }

        public async Task ReconnectAsync()
        {
            if (Stopping)
                return;

            Reconnecting = true;
            await Task.Delay(1000);
            Log.Info("Reconnecting...");
            try
            {
                try
                {
                    await Client.StopAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Log.Warn("Error at Client.StopAsync | {0}", ex);
                }

                try { await Exit().ConfigureAwait(false); } catch(Exception ex) { Log.Warn("Error at Exit | {0}", ex); }
                try { PlayingStatusHandler.Stop(); } catch (Exception ex) { Log.Warn("Error at PlayingStatusHandler.Stop | {0}", ex); }
                try { PlayingStatusHandler.Clear(); } catch (Exception ex) { Log.Warn("Error at PlayingStatusHandler.Clear | {0}", ex); }
                try { CommandHandler?.Dispose(); } catch (Exception ex) { Log.Warn("Error at CommandHandler.Dispose | {0}", ex); }
                try { ReactionHandler?.Dispose(); } catch (Exception ex) { Log.Warn("Error at ReactionHandler.Dispose | {0}", ex); }
                try { Client?.Dispose(); } catch (Exception ex) { Log.Warn("Error at Client.Dispose | {0}", ex); }
                Running = false;

                if(!await RunAsync().ConfigureAwait(false))
                {
                    throw new InvalidOperationException("Unable to connect");
                }

                await Task.Delay(10000).ConfigureAwait(false);
                if (Client.CurrentUser == null)
                {
                    throw new InvalidOperationException("Failed to validate discord connection.");
                }

                Log.Info("Bot has been reconnected to the server.");
            }
            catch (Exception ex)
            {
                Log.Error("Could not reconnect, retrying in 1 minute", ex);
                await Task.Delay(TimeSpan.FromMinutes(1)).ConfigureAwait(false);
                //await ReconnectAsync().ConfigureAwait(false);
                var _ = Task.Run(() => ReconnectAsync());
            }
            Reconnecting = false;
        }

        private static IServiceProvider CreateServiceProvider()
        {
            var config = new DiscordSocketConfig()
            {
                MessageCacheSize = Settings.Cache.AmountOfCachedMessages,
                LogLevel = LogSeverity.Warning,
                ConnectionTimeout = (int)(Settings.Timeout * 60),
                HandlerTimeout = (int)(Settings.Timeout * 60),
                DefaultRetryMode = RetryMode.AlwaysRetry,
                GatewayIntents = GatewayIntents.All,
            };

            var serviceConfig = new InteractionServiceConfig()
            {
                LogLevel = LogSeverity.Warning,
                DefaultRunMode = RunMode.Async,
                UseCompiledLambda = true,
                AutoServiceScopes = true,
            };

            var collection = new ServiceCollection()
                .AddSingleton(config)
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton(serviceConfig)
                .AddSingleton<InteractionService>()
                ;
            return collection.BuildServiceProvider();
        }

        public async Task<bool> RunAsync()
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
                // Write the settings again to update it with any new properties that might have been added.
                await Settings.WriteAsync("data/settings.xml").ConfigureAwait(false);
            }
            catch(Exception ex)
            {
                Log.Fatal(ex);
                return false;
            }

            // Try to initialize the database
            try
            {
                (Database = new DatabaseHandler()).Setup(Settings.Credentials.Sql.Type, Settings.Credentials.Sql.ConnectionString);
            }
            catch(Exception ex)
            {
                Log.Fatal("Unable to create a connection with the database, please check the file \"/data/settings.xml\"", ex);
                return false;
            }

            // Initialize the providers
            ServiceProvider = CreateServiceProvider();

            // Try to initialise the service 'Google'
            try
            {
                await (Google = new GoogleService()).SetupAsync(Settings.Credentials.GoogleApiKey).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Error("Could not initialize Google {0}\n", ex.ToString());
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

            // Try to initialise 'Twitch'
            try
            {
                if (string.IsNullOrEmpty(Settings.Credentials.TwitchApiSecret))
                {
                    Log.Info("Skipping Twitch init");
                }
                else
                {
                    Twitch = new TwitchLib.Api.TwitchAPI();
                    Twitch.Settings.ClientId = Settings.Credentials.TwitchApiClientId;
                    Twitch.Settings.Secret = Settings.Credentials.TwitchApiSecret;
                    var twitchResult = await Twitch.V5.Auth.CheckCredentialsAsync().ConfigureAwait(false);
                    if (Twitch.Settings.ClientId == null || !twitchResult.Result)
                    {
                        Twitch = null;
                        throw new ApiException("Twitch credentials check failed.");
                    }
                }
            }
            catch(Exception ex)
            {
                Log.Warn("Could not initialize Twitch {0}\n", ex.ToString());
            }

            // Create our discord client
            Client = ServiceProvider.GetRequiredService<DiscordSocketClient>();
            InteractionService = ServiceProvider.GetRequiredService<InteractionService>();

            await InteractionService.AddModulesAsync(typeof(Ditto).Assembly, ServiceProvider);
            
            // Various services
            if (Cache == null)
            {
                (Cache = new CacheHandler()).Setup(TimeSpan.FromSeconds(Settings.Cache.CacheTime));
            }

            CommandHandler?.Dispose();
            await (CommandHandler = new CommandHandler(Client)).SetupAsync().ConfigureAwait(false);

            if (_firstStart)
            {
                Client.Connected += async () =>
                {
                    if (_firstStart)
                    {
                        // Setup services
                        await CommandHandler.SetupAsync().ConfigureAwait(false);
                        ReactionHandler?.Dispose();
                        await (ReactionHandler = new ReactionHandler()).SetupAsync(Client).ConfigureAwait(false);
                        PlayingStatusHandler.Setup(TimeSpan.FromMinutes(1));
                    }

                    _firstStart = false;
                };

                Connected += async () =>
                {
                    // Start services
                    await CommandHandler.StartAsync().ConfigureAwait(false);
                    PlayingStatusHandler.Start();

                    Connected -= Initialised;
                };

                Client.InteractionCreated += async (interaction) =>
                {
                    var context = new SocketInteractionContext(Client, interaction);
                    await InteractionService.ExecuteCommandAsync(context, ServiceProvider);
                };

                // Call this once after a successful connection.
                Connected += Initialised;

                Client.Ready += async () =>
                {
                    if (!Running)
                    {
                        Running = true;
                        await Connected().ConfigureAwait(false);
                    }
                    Log.Info("Connected");

                    if (Type == BotType.Bot)
                    {
                        await Client.SetGameAsync(null);
                        await Client.SetStatusExAsync(UserStatusEx.Online);
                    }

                    try
                    {
                        await InteractionService.RegisterCommandsGloballyAsync();
                    }
                    catch (Exception ex)
                    {
                        Log.Fatal(ex);
                        throw;
                    }
                };

                Client.Disconnected += (e) =>
                {
                    if (Reconnecting)
                    {
                        //_reconnecting = false;
                    }
                    else
                    {
                        Log.Warn("Bot has been disconnected. {0}", (object)(Exiting ? null : $"| {e}"));
                        if (!Exiting && Settings.AutoReconnect && !Reconnecting)
                        {
                            var _ = Task.Run(() => ReconnectAsync());
                        }
                    }
                    return Task.CompletedTask;
                };

                Client.LoggedOut += () =>
                {
                    Log.Warn("Bot has logged out.");
                    if (!Exiting && Settings.AutoReconnect && !Reconnecting)
                    {
                        var _ = Task.Run(() => ReconnectAsync());
                    }
                    return Task.CompletedTask;
                };
            }

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
                return false;
            }

            await Client.StartAsync();
            return true;
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