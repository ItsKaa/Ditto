using Cauldron.Core.Collections;
using Discord;
using Discord.Audio;
using Ditto.Bot.Services.Data;
using Ditto.Data;
using Ditto.Data.Commands;
using Ditto.Data.Discord;
using Ditto.Extensions;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ditto.Bot.Modules.Music.Data
{
    internal class MusicPlayer : BaseClass, IDisposable
    {
        private ICommandContextEx Context { get; set; }
        private IAudioClient AudioClient { get; set; }
        private IVoiceChannel VoiceChannel { get; set; }
        private AudioStreamer AudioStreamer { get; set; }
        private MusicController MusicController { get; set; }
        public bool Running { get; private set; }
        public bool RepeatSong { get; set; }
        public bool RepeatPlaylist { get; set; }
        public bool RandomSong { get; set; }
        public int CurrentIndex { get; private set; }
        public PlaylistItem? Current { get; private set; }
        public ConcurrentList<PlaylistItem> Playlist { get; private set; }
        public delegate void SongChangedHandler(PlaylistItem? oldItem, PlaylistItem? newItem);
        
        public event SongChangedHandler SongChanging;
        public event SongChangedHandler SongChanged;

        // Getters/Redirected properties
        public bool Paused => AudioStreamer?.Paused ?? true;
        public TimeSpan TimeElapsed => AudioStreamer?.TimeElapsed ?? TimeSpan.Zero;
        public double Volume
        {
            get => AudioStreamer?.Volume ?? double.NaN;
            set
            {
                if (AudioStreamer != null)
                {
                    AudioStreamer.Volume = value;
                }
            }
        }

        // Fields
        private CancellationTokenSource _cancellationTokenSource;
        private bool _disposed = false;

        public MusicPlayer(ICommandContextEx context)
        {
            Context = context;
            AudioClient = null;
            VoiceChannel = null;
            Running = false;
            RepeatSong = false;
            RepeatPlaylist = false;
            RandomSong = false;
            CurrentIndex = 0;
            Current = null;
            Playlist = new ConcurrentList<PlaylistItem>();
            Volume = 100.0;

            MusicController = new MusicController(this, Context.Channel, Context.Guild);
            MusicController.Add(
                new MusicControllerItem(Emotes.PlayPause, () => PauseBroadcastAsync()),
                new MusicControllerItem(Emotes.TrackPrevious, PreviousSongAsync),
                new MusicControllerItem(Emotes.ArrowLeft, PreviousPageAsync),
                new MusicControllerItem(Emotes.StopButton, DisconnectAndDisposeClientAsync),
                new MusicControllerItem(Emotes.ArrowRight, NextPageAsync),
                new MusicControllerItem(Emotes.TrackNext, NextSongAsync),

                new MusicControllerItem(Emotes.Repeat, () => { RepeatPlaylist = !RepeatPlaylist; return Task.CompletedTask; }, display: true),
                new MusicControllerItem(Emotes.RepeatOne, () => { RepeatSong = !RepeatSong; return Task.CompletedTask; }, display: true),
                new MusicControllerItem(Emotes.TwistedRightwardsArrows, () => { RandomSong = !RandomSong; return Task.CompletedTask; }, display: true),
                new MusicControllerItem(Emotes.HeavyPlusSign, () => { Volume += 10; return Task.CompletedTask; }),
                new MusicControllerItem(Emotes.HeavyMinusSign, () => { Volume -= 10; return Task.CompletedTask; })
            );

            Initialise();
        }
        private void Initialise()
        {
            CurrentIndex = 0;
            Current = null;
            Playlist = new ConcurrentList<PlaylistItem>();
            AudioStreamer = new AudioStreamer(
                Ditto.Settings.Paths.YoutubeDL,
                Ditto.Settings.Paths.FFmpeg,
                "--prefer-ffmpeg --hls-prefer-ffmpeg --buffer-size 100M --audio-quality 48K --format bestaudio", // --format bestaudio --force-ipv4
                "-ac 2 -f s16le -ar 48000"
            );
        }
       
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Running = false;
                    DisconnectAndDisposeClientAsync().GetAwaiter().GetResult();
                }
                _disposed = true;
            }
        }

        public async Task DisconnectAndDisposeClientAsync()
        {
            Running = false;
            try { await StopBroadcastingAsync().ConfigureAwait(false); } catch { }
            try { _cancellationTokenSource?.Cancel(); } catch { }
            try { await VoiceChannel.DisconnectAsync().ConfigureAwait(false); } catch { }
            try {
                if (AudioClient != null)
                {
                    await AudioClient.StopAsync().ConfigureAwait(false);
                }

                AudioClient?.Dispose();
                AudioClient = null;
            } catch { }

            try
            {
                await (MusicController?.StopAsync()).ConfigureAwait(false);
            }
            catch { }

            try { AudioStreamer?.StopStreaming(); } catch { }
            try { AudioStreamer?.Dispose(); } catch { }
            AudioStreamer = null;

            // Wait a little bit just in case some of the tasks are still running.
            await Task.Delay(100).ConfigureAwait(false);
            Playlist.Clear();
            Current = null;
            CurrentIndex = 0;

            Initialise();
        }

        public async Task ShufflePlaylist()
        {
            Playlist.Shuffle();
            CurrentIndex = -1;
            await StopBroadcastingAsync().ConfigureAwait(false);
            await MusicController.UpdateAsync().ConfigureAwait(false);
        }


        public Task ReconnectAsync()
        {
            Log.Warn("TODO: ReconnectAsync()");
            return Task.CompletedTask;
        }

        private void Start()
        {
            if (!Running)
            {
                Running = true;
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource = new CancellationTokenSource();
                var task = Task.Run(async () =>
                {
                    while (Running && _cancellationTokenSource?.IsCancellationRequested == false)
                    {
                        if (Playlist.Count == 0 || Playlist.Count <= CurrentIndex)
                        {
                            if (RepeatPlaylist && Playlist.Count > 0)
                            {
                                CurrentIndex = 0;
                            }
                            else
                            {
                                // For the moment, just stop running.
                                Log.Info("Playlist empty, aborting...");
                                break;
                            }
                        }
                        else
                        {
                            var item = Playlist.ElementAtOrDefault(CurrentIndex);
                            if (!Equals(item, default(PlaylistItem)))
                            {
                                SongChanged?.Invoke(Current, item);
                                await BroadcastAudioAsync(item).ConfigureAwait(false);
                            }
                            var newIndex = 0;
                            if (!RepeatSong)
                            {
                                if (RandomSong)
                                {
                                    newIndex = new Randomizer().New(0, Playlist.Count - 1);
                                }
                                else
                                {
                                    newIndex = CurrentIndex + 1;
                                }
                            }

                            PlaylistItem? newItem = Playlist.ElementAtOrDefault(newIndex);
                            if(Equals(newItem, default(PlaylistItem)))
                            {
                                newItem = null;
                            }
                            SongChanging?.Invoke(item, newItem);
                            CurrentIndex = newIndex;
                        }
                    }

                    await DisconnectAndDisposeClientAsync().ConfigureAwait(false);
                    Log.Info("MusicPlayer stopped.");

                }, _cancellationTokenSource.Token);

                // Begin!
                MusicController.Start();
            }
        }

        public Task<YoutubeResult> GetSongAsync(string query)
        {
            return Ditto.Google.Youtube.SearchAsync(query);
        }

        public async Task ConnectAndPlayAsync(IVoiceChannel voiceChannel, IGuildUser guildUser, string query)
        {
            // Determine whether we need to (re)join the selected voice channel.
            if (VoiceChannel == null || AudioClient == null
                || (AudioClient.ConnectionState == ConnectionState.Disconnected || AudioClient.ConnectionState == ConnectionState.Disconnecting)
                || !voiceChannel.Id.Equals(VoiceChannel?.Id)
                || await voiceChannel.GetUserAsync((await Ditto.Client.DoAsync(c => c.CurrentUser).ConfigureAwait(false)).Id) == null
            )
            {
                VoiceChannel = voiceChannel;
                await JoinVoiceAsync().ConfigureAwait(false);
            }

            var youtubeResult = await GetSongAsync(query).ConfigureAwait(false);
            if (!youtubeResult.Equals(YoutubeResult.Invalid))
            {
                Playlist.AddRange(youtubeResult.Songs.Select(song => new PlaylistItem()
                {
                    GuildUser = guildUser,
                    Song = song
                }));
                await MusicController.UpdateAsync().ConfigureAwait(false);
            }
            else
            {
                Log.Info($"Could not find a {nameof(YoutubeResult)} for query \"{query}\".");
            }

            Start();
        }


        public async Task JoinVoiceAsync()
        {
            await DisconnectAndDisposeClientAsync().ConfigureAwait(false);
            AudioClient = (await VoiceChannel.ConnectAsync(true, false, false).ContinueWith((task) =>
            {
                var audioClient = task.Result;
                audioClient.Connected += () =>
                {
                    Log.Info("Audio connected.");
                    return Task.CompletedTask;
                };
                audioClient.Disconnected += (ex) =>
                {
                    //Log.Info("Audio disconnected", ex);
                    return Task.CompletedTask;
                };
                audioClient.StreamCreated += (id, audioInStream) =>
                {
                    //Log.Info($"Audio Stream #{id} created.");
                    return Task.CompletedTask;
                };
                audioClient.StreamDestroyed += (id) =>
                {
                    //Log.Info($"Audio Stream #{id} destroyed.");
                    return Task.CompletedTask;
                };
                audioClient.SpeakingUpdated += (id, value) =>
                {
                    //Log.Info($"Audio speaking updated #{id} = {value}");
                    return Task.CompletedTask;
                };
                return task.Result;

            }).ConfigureAwait(false));
        }
        
        private async Task BroadcastAudioAsync(PlaylistItem? playlistItem = null)
        {
            if (playlistItem.HasValue)
            {
                Current = playlistItem;
                var _ = MusicController.UpdateAsync();
                try
                {
                    await AudioStreamer.StreamAsync(
                        playlistItem?.Song.Url,
                        AudioClient.CreatePCMStream(AudioApplication.Music, 98304)
                    ).ConfigureAwait(false);
                }
                catch(Exception ex)
                {
                    Log.Error(ex);
                }

                if (AudioStreamer != null && AudioStreamer.Running // I THINK
                    && Current.HasValue && Current.Value.Song.Duration.HasValue)
                {
                    var durationDifference = (Current.Value.Song.Duration.Value - AudioStreamer?.TimeElapsed).Value.TotalSeconds;
                    if (durationDifference >= 10)
                    {
                        Log.Warn($"Detected a difference in song duration: {durationDifference} | {Ditto.Exiting} | {Ditto.Reconnecting}");
                    }
                }
            }
            else
            {
                AudioStreamer?.StopStreaming();
            }
        }

        public Task StopBroadcastingAsync() => BroadcastAudioAsync(null);
        
        public Task PauseBroadcastAsync(bool? pause = null)
        {
            AudioStreamer.Pause(pause ?? !AudioStreamer.Paused);
            return Task.CompletedTask;
        }


        public async Task NavigateSongAsync(int amount = 1)
        {
            if (amount != 0)
            {
                if (Playlist.Count == 0)
                {
                    CurrentIndex = 0;
                }
                else
                {
                    if (amount > 0)
                    {
                        // next
                        CurrentIndex += (amount - 1);
                        if (CurrentIndex + 1 >= Playlist.Count)
                        {
                            CurrentIndex = -1;
                        }
                        await StopBroadcastingAsync().ConfigureAwait(false);
                    }
                    else
                    {
                        // previous
                        CurrentIndex += amount - 1; // -1 because it'll automatically increase once a song finishes.
                        if (CurrentIndex < -1)
                        {
                            CurrentIndex = Playlist.Count - 2; // because +1
                        }
                        await StopBroadcastingAsync().ConfigureAwait(false);
                    }
                }
            }
        }
        public Task PreviousSongAsync() => NavigateSongAsync(-1);
        public Task NextSongAsync() => NavigateSongAsync(+1);

        private Task NavigatePageAsync(int pageAmount)
        {
            MusicController.CurrentPage += pageAmount;
            return Task.CompletedTask;
        }

        private Task PreviousPageAsync() => NavigatePageAsync(-1);
        private Task NextPageAsync() => NavigatePageAsync(+1);

        public void ScrollToIndex(int index) => MusicController.ScrollToIndex(index);
    }
}
