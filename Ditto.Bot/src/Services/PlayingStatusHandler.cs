using Ditto.Bot.Services.Data;
using Ditto.Data.Discord;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ditto.Bot.Services
{
    public static class PlayingStatusHandler
    {
        private static ConcurrentQueue<PlayingStatusItem<Task<string>>> _playingStatusItems = new ConcurrentQueue<PlayingStatusItem<Task<string>>>();
        private static CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private static int _idCounter = 0;
        private static DateTime _lastTime = DateTime.MinValue;

        public static bool Running { get; private set; } = false;
        public static TimeSpan Delay { get; private set; } = TimeSpan.FromMinutes(1);
        public static bool Initialized { get; private set; } = false;
        
        public static void Setup(TimeSpan delay)
        {
            Delay = delay;
        }

        public static void Start()
        {
            if (!Initialized)
            {
                Initialized = true;
                if (_cancellationTokenSource.IsCancellationRequested)
                {
                    _cancellationTokenSource = new CancellationTokenSource();
                    Running = false;
                }
                if (!Running)
                {
                    Running = true;
                    try
                    {
                        var _ = Task.Run(async () =>
                        {
                            while (Running)
                            {
                                try
                                {
                                    if ((DateTime.Now - _lastTime) > Delay)
                                    {
                                        // Sort items based on priority and date added
                                        //if (Ditto.Running && Ditto.Client != null
                                        //        && Ditto.Client.ConnectionState == ConnectionState.Connected
                                        //        && Ditto.Client.LoginState == LoginState.LoggedIn)
                                        //{
                                        //    var items = _playingStatusItems.OrderByDescending(e => e.Priority).ThenByDescending(e => e.DateAdded);
                                        //    foreach (var item in items)
                                        //    {
                                        //        var result = await item.Execute(Ditto.Client).ConfigureAwait(false);
                                        //        if (result != null)
                                        //        {
                                        //            if (Ditto.Running && Ditto.Client != null
                                        //                && Ditto.Client.ConnectionState == ConnectionState.Connected
                                        //                && Ditto.Client.LoginState == LoginState.LoggedIn)
                                        //            {
                                        //                await Ditto.Client.SetGameAsync(result.Length == 0 ? null : result);
                                        //            }
                                        //            break;
                                        //        }
                                        //    }
                                        //}
                                        
                                        if (Ditto.IsClientConnected())
                                        {
                                            var items = _playingStatusItems.OrderByDescending(e => e.Priority).ThenByDescending(e => e.DateAdded);
                                            foreach (var item in items)
                                            {
                                                var result = await item.Execute(Ditto.Client).ConfigureAwait(false);
                                                if (result != null)
                                                {
                                                    if (Ditto.IsClientConnected())
                                                    {
                                                        await Ditto.Client.SetGameAsync(result.Length == 0 ? null : result);
                                                    }
                                                    break;
                                                }
                                            }
                                        }
                                        _lastTime = DateTime.Now;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Log.Error(ex);
                                }
                                await Task.Delay(100).ConfigureAwait(false);
                            }
                        }, _cancellationTokenSource.Token);
                    }
                    catch (Exception ex) when (ex is OperationCanceledException || ex is TaskCanceledException)
                    {
                        if (_cancellationTokenSource.IsCancellationRequested)
                        {
                            _cancellationTokenSource = new CancellationTokenSource();
                        }
                    }
                }
            }
        }
        public static void Stop()
        {
            Initialized = false;
            Running = false;
            _cancellationTokenSource.Cancel();
        }
        public static void Clear()
        {
            _playingStatusItems.Clear();
        }

        public static void Register(PriorityLevel priorityLevel, Func<DiscordClientEx, Task<string>> func)
        {
            _playingStatusItems.Enqueue(new PlayingStatusItem<Task<string>>(_idCounter++)
            {
                Priority = priorityLevel,
                Function = func,
                //Delay = delay
            });
        }

        public static void Register(PriorityLevel priorityLevel, Func<DiscordClientEx, string> func)
            => Register(priorityLevel, (client) => Task.FromResult(func(client)));
    }
}
