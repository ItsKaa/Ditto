using Discord;
using Discord.Net;
using Ditto.Data;
using Ditto.Data.Discord;
using Ditto.Extensions;
using Ditto.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ditto.Bot.Modules.Music.Data
{
    internal class MusicController : IDisposable
    {
        public IGuild Guild { get; private set; }
        public IMessageChannel Channel { get; private set; }
        public MusicPlayer MusicPlayer { get; private set; }
        public List<MusicControllerItem> ControllerItems { get; set; }
        public TimeSpan UpdateFrequency { get; set; }
        private bool Running { get; set; }
        private CancellationTokenSource CancellationTokenSource { get; set; }
        public CancellationToken CancellationToken => CancellationTokenSource?.Token ?? CancellationToken.None;
        private bool HasCurrentSongInPage { get; set; }
        private const int MaxPageSize = 50;
        private int _pageItemCount;
        private int _pageCount;
        private int _currentPage;
        
        public int PageItemCount
        {
            get => _pageItemCount;
            set
            {
                if (value < 1)
                    value = 1;
                if (value > MaxPageSize)
                    value = MaxPageSize;
                if(MusicPlayer?.Current != null)
                {
                    if(HasCurrentSongInPage)
                    {
                        ScrollToIndex(MusicPlayer.CurrentIndex);
                    }
                    else
                    {
                        ScrollToIndex((CurrentPage-1) * PageItemCount);
                    }
                }
                _pageItemCount = value;
            }
        }
        public int PageCount
        {
            get => _pageCount;
            private set
            {
                if (value < 1)
                {
                    value = 1;
                }
                _pageCount = value;
                if (CurrentPage > _pageCount)
                {
                    CurrentPage = value;
                }
            }
        }
        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                if(value < 1)
                {
                    //value = 1;
                    value = PageCount;
                }
                if(value > PageCount)
                {
                    //value = PageCount;
                    value = 1;
                }
                _currentPage = value;
            }
        }

        private Task _updateTask = Task.CompletedTask;
        private IUserMessage _userMessage;

        public MusicController(MusicPlayer musicPlayer, IMessageChannel channel, IGuild guild, TimeSpan? updateFrequency = null)
        {
            Running = false;
            Guild = guild;
            Channel = channel;
            MusicPlayer = musicPlayer;
            MusicPlayer.SongChanging += (oldItem, newItem) =>
            {
                if(HasCurrentSongInPage && oldItem != null && newItem != null)
                {
                    ScrollToIndex(MusicPlayer.Playlist.IndexOf(newItem.Value));
                }
            };
            PageItemCount = 5;
            PageCount = 1;
            CurrentPage = 1;
            HasCurrentSongInPage = false;
            UpdateFrequency = updateFrequency ?? TimeSpan.FromSeconds(15);
            ControllerItems = new List<MusicControllerItem>();
            CancellationTokenSource = new CancellationTokenSource();
        }

        public void ScrollToIndex(int index)
        {
            var oldPage = CurrentPage;
            CurrentPage = (index / PageItemCount) + 1;
            if (CurrentPage != oldPage)
            {
                var _ = UpdateAsync();
            }
        }

        public void Dispose()
        {
            StopAsync().Wait();
        }

        public void Start()
        {
            Running = true;
            if (CancellationTokenSource?.IsCancellationRequested != false)
            {
                CancellationTokenSource = new CancellationTokenSource();
            }
            _updateTask = Task.Run(async () =>
            {
                while (Running && MusicPlayer != null)
                {
                    await Task.Delay(UpdateFrequency, CancellationToken).ConfigureAwait(false);
                    await UpdateAsync().ConfigureAwait(false);
                }
            }, CancellationToken);
        }

        public async Task StopAsync()
        {
            Running = false;
            try { CancellationTokenSource?.Cancel(); } catch { }
            try { _updateTask.Dispose(); } catch { }
            if (_userMessage != null)
            {
                try { await _userMessage.DeleteAsync().ConfigureAwait(false); } catch { }
                _userMessage = null;
            }
        }


        public void Add(params MusicControllerItem[] controllerItems)
        {
            ControllerItems.AddRange(controllerItems);
        }
        
        public async Task UpdateAsync()
        {
            try
            {
                if (MusicPlayer != null)
                {
                    if (MusicPlayer.Running && MusicPlayer.Current.HasValue)
                    {
                        //var current = MusicPlayer.Current.Value;
                        var footerDisplayString = ControllerItems.Where(e => e.Display && e.IsEnabled).Select(e => EmotesHelper.GetString(e.Emote)).Flatten(" ");

                        var embedBuilder = new EmbedBuilder()
                            .WithAuthor(new EmbedAuthorBuilder()
                                // Music Player (Paused): [15/200]
                                .WithName($"Music Player{(MusicPlayer.Paused ? " (Paused)" : "")}: [{MusicPlayer.CurrentIndex + 1}/{MusicPlayer.Playlist.Count}]")
                                .WithIconUrl(
                                    MusicPlayer.Paused ? "https://i.imgur.com/W0NgMmO.png" : "https://i.imgur.com/cmSTgC4.png"
                                )
                            )
                            .WithThumbnailUrl(MusicPlayer.Current?.Song.ThumbnailUrl)
                            .WithFooter(new EmbedFooterBuilder()
                                .WithIconUrl(MusicPlayer.Current?.GuildUser?.GetAvatarUrl())
                                .WithText(
                                    $"{(MusicPlayer.Volume >= 50 ? "🔊" : "🔉")} {MusicPlayer.Volume:0}%"
                                    + (footerDisplayString.Length > 0 ? $" | {footerDisplayString}" : "")
                                    + $" | {CurrentPage}/{PageCount}"
                                )
                            )
                            .WithColor(MusicPlayer.Paused
                                ? Ditto.Cache.Db.EmbedMusicPausedColour(Guild)
                                : Ditto.Cache.Db.EmbedMusicPlayingColour(Guild)
                            );

                        // Description
                        if (MusicPlayer.Current.HasValue)
                        {
                            embedBuilder = embedBuilder.WithDescription(
                                $"__**Currently playing:**__" +
                                "```" +
                                $"🎵 {MusicPlayer.Current?.Song.Title.TrimTo(40)} 🎵\n" +
                                //$"Remaining: {(MusicPlayer.Current?.Song.Duration?.Humanize(true, " ") ?? "##:##")}\n" +
                                $"Requested by {MusicPlayer.Current?.GuildUser.Nickname ?? MusicPlayer.Current?.GuildUser.Username}\n" +
                                (MusicPlayer.Current.HasValue ?
                                    (MusicPlayer.Current?.Song?.LiveStream == true ? $"🔴 Live [{TimeSpan.Zero.ToShortString(TimeUnit.Minutes | TimeUnit.Seconds)}]"
                                    : $"{(MusicPlayer.TimeElapsed.ToShortString(TimeUnit.Minutes | TimeUnit.Seconds))}"
                                        + (MusicPlayer.Current?.Song?.LiveStream == false ? "" : " / "
                                        + (MusicPlayer.Current?.Song.Duration?.ToShortString(TimeUnit.Minutes | TimeUnit.Seconds) ?? "##:##")
                                    ))
                                    : ""
                                ) +
                                "```" +
                                //$"{new string('═', 35)}\n" +
                                $"\n__**Playlist:**__\n"
                            );
                        }


                        PageCount = (int)Math.Ceiling(MusicPlayer.Playlist.Count / (double)PageItemCount);

                        //var i = 1;
                        var hasSelectedSong = false;
                        var i = (CurrentPage - 1) * PageItemCount + 1;
                        foreach (var item in MusicPlayer.Playlist.Skip((CurrentPage - 1) * PageItemCount).Take(PageItemCount))
                        {
                            var selectedString = "";
                            if (i - 1 == MusicPlayer.CurrentIndex)
                            {
                                selectedString = "\\🎵";
                                hasSelectedSong = true;
                            }
                            embedBuilder.AddField(
                                $@"{i++}. {selectedString} {item.Song.Title} {selectedString}",
                                $"  *{(item.Song.Duration?.ToString() ?? "##:##")} | {item.GuildUser.Nickname ?? item.GuildUser.Username}*",
                                false
                            );
                        }
                        HasCurrentSongInPage = hasSelectedSong;

                        if (_userMessage == null)
                        {
                            _userMessage = await Channel.EmbedAsync(embedBuilder, Guild).ConfigureAwait(false);
                            var botUserId = Ditto.Client.CurrentUser.Id;
                            Ditto.ReactionHandler.Add(_userMessage,
                                async r =>
                                {
                                    if (r?.Emote != null && r.UserId != botUserId)
                                    {
                                        var update = false;
                                        foreach (var item in ControllerItems.Where(e => EmotesHelper.GetString(e.Emote) == r.Emote.Name))
                                        {
                                            if (item.Add != null)
                                            {
                                                await item.Add().ConfigureAwait(false);
                                                update = true;
                                            }
                                            if (item.AddOrRemove != null)
                                            {
                                                await item.AddOrRemove().ConfigureAwait(false);
                                                update = true;
                                            }
                                        }
                                        if (update)
                                        {
                                            await UpdateAsync().ConfigureAwait(false);
                                        }
                                    }
                                },
                                async r =>
                                {
                                    if (r.UserId != botUserId)
                                    {
                                        var update = false;
                                        foreach (var item in ControllerItems.Where(e => EmotesHelper.GetString(e.Emote) == r.Emote.Name))
                                        {
                                            if (item.Remove != null)
                                            {
                                                await item.Remove().ConfigureAwait(false);
                                                update = true;
                                            }
                                            if (item.AddOrRemove != null)
                                            {
                                                await item.AddOrRemove().ConfigureAwait(false);
                                                update = true;
                                            }
                                        }
                                        if (update)
                                        {
                                            await UpdateAsync().ConfigureAwait(false);
                                        }
                                    }
                                }
                            );

                            foreach (var item in ControllerItems)
                            {
                                if (_userMessage != null)
                                {
                                    await _userMessage.AddReactionAsync(EmotesHelper.GetEmoji(item.Emote), new RequestOptions() { RetryMode = RetryMode.AlwaysRetry }).ConfigureAwait(false);
                                }
                            }
                        }
                        else
                        {
                            await _userMessage.ModifyAsync(m => m.Embed = embedBuilder.Build(), new RequestOptions() { RetryMode = RetryMode.AlwaysRetry }).ConfigureAwait(false);
                        }

                    }
                }
                else
                {
                    // MusicPlayer == null
                }
            }
            catch (OperationCanceledException) { }
            catch (HttpException) { }
            catch (RateLimitedException)
            {
                await UpdateAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }
    }
}
