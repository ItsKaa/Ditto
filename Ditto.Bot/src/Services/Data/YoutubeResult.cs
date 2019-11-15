using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ditto.Bot.Services.Data
{
    public class SongInfo : IEquatable<SongInfo>
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public bool LiveStream { get; set; }
        public TimeSpan? Duration { get; set; }
        public bool DetailsLoaded { get; private set; }
        public string Url => Globals.Strings.YoutubeVideoUrl + Id;
        public string ThumbnailUrl => $"{Globals.Strings.YoutubeVideoThumbnailUrlStart}{Id}/{Globals.Strings.YoutubeThumnailUrlFileName_Small}{(LiveStream ? "_live" : "")}.jpg";

        public static readonly SongInfo Invalid = new SongInfo()
        {
            Id = null,
            Title = null,
            Duration = null,
            DetailsLoaded = false,
            LiveStream = false,
        };

        public SongInfo SetDetails(string title, bool isLiveStream, TimeSpan duration)
        {
            Title = title;
            LiveStream = isLiveStream;
            Duration = duration;
            DetailsLoaded = true;
            return this;
        }

        public bool Equals(SongInfo other)
        {
            return Title.Equals(other.Title) &&
                Id.Equals(other.Id) &&
                Duration.Equals(other.Duration) &&
                DetailsLoaded == other.DetailsLoaded &&
                LiveStream == other.LiveStream
                ;
        }
    }

    public struct PlaylistInfo : IEquatable<PlaylistInfo>
    {
        public string Title { get; set; }
        public int SongCount { get; set; }
        public static readonly PlaylistInfo Invalid = new PlaylistInfo()
        {
            Title = null,
            SongCount = -1
        };

        public bool Equals(PlaylistInfo other)
        {
            return Title.Equals(other.Title) &&
                SongCount == other.SongCount;
        }
    }
    
    public enum YoutubeResultType
    {
        None,
        Video,
        Playlist
    }

    public class YoutubeResult : IEquatable<YoutubeResult>, IDisposable
    {
        public static readonly YoutubeResult Invalid = new YoutubeResult()
        {
            Id = null,
            SongsLoaded = false,
            Type = YoutubeResultType.None,
            Songs = Enumerable.Empty<SongInfo>().ToList(),
            PlaylistInfo = PlaylistInfo.Invalid,
        };

        public string Id { get; set; }
        public YoutubeResultType Type { get; set; }
        public bool SongsLoaded { get; set; }
        public bool SongDetailsLoaded { get; private set; }
        public PlaylistInfo PlaylistInfo { get; set; }
        public List<SongInfo> Songs { get; set; }
        private Task _detailsLoadingTask;
        private CancellationTokenSource _cancellationTokenSource;
        public CancellationToken CancellationToken => _cancellationTokenSource?.Token ?? CancellationToken.None;


        public YoutubeResult()
        {
            Id = null;
            Type = YoutubeResultType.None;
            Songs = Enumerable.Empty<SongInfo>().ToList();
            PlaylistInfo = new PlaylistInfo();
            SongsLoaded = false;
            SongDetailsLoaded = false;
            _detailsLoadingTask = null;
            _cancellationTokenSource = new CancellationTokenSource();
        }
        
        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
        }

        public async Task<YoutubeResult> LoadSongsAsync(bool loadDetails = true)
        {
            if (Type == YoutubeResultType.Playlist)
            {
                if (!SongsLoaded)
                {
                    var token = "";
                    do
                    {
                        token = await Ditto.Google.Youtube.ReadPlaylistSongsBasicAsync(this, token).ConfigureAwait(false);
                    }
                    while (!string.IsNullOrEmpty(token));
                    SongsLoaded = true;
                }
                if (loadDetails)
                {
                    _detailsLoadingTask = Task.Run(async () =>
                    {
                        await Ditto.Google.Youtube.ReadPlaylistSongDetailsAsync(this).ConfigureAwait(false);
                        SongDetailsLoaded = true;
                    }, CancellationToken);
                }
            }
            else if(Type == YoutubeResultType.Video)
            {
                if(!SongsLoaded || !SongDetailsLoaded)
                {
                    var song = await Ditto.Google.Youtube.GetVideoByIdAsync(Id);
                    if (Songs.Count > 0)
                    {
                        Songs[0] = song;
                    }
                    else
                    {
                        Songs.Add(song);
                    }
                    SongsLoaded = true;
                    SongDetailsLoaded = true;
                }
            }
            return this;
        }


        public bool Equals(YoutubeResult other)
        {
            return
                Id.Equals(other.Id) &&
                Type == other.Type &&
                Songs.SequenceEqual(other.Songs) &&
                SongsLoaded == other.SongsLoaded &&
                PlaylistInfo.Equals(other.PlaylistInfo)
                ;
        }

    }
}
