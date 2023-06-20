using Ditto.Bot.Services.Data;
using Ditto.Extensions;
using Google;
using Google.Apis.YouTube.v3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace Ditto.Bot.Services
{
    public class YoutubeService : IDittoService
    {
        private GoogleService GoogleService { get; }
        protected YouTubeService _service { get; set; }

        public YoutubeService(GoogleService googleService)
        {
            GoogleService = googleService;
        }

        public Task Initialised() => Task.CompletedTask;

        public async Task Connected()
        {
            try
            {
                _service = new YouTubeService(GoogleService.BaseClientService);
                await GetPlaylistNameAsync("0");
            }
            catch (GoogleApiException)
            {
                Log.Warn("Missing or incorrect Google API Key, all commands dependent on Youtube will not work.");
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        public Task Exit() => Task.CompletedTask;

        protected bool IsValid(Regex regex, string url, out string id)
        {
            id = null;
            var match = regex.Match(url);
            if (match.Success)
            {
                id = match.Groups["id"].Value;
            }
            return match.Success;
        }

        public bool IsValidPlaylist(string url, out string id)
            => IsValid(Globals.RegularExpression.YoutubePlaylistId, url, out id);

        public bool IsValidPlaylist(string url)
            => IsValid(Globals.RegularExpression.YoutubePlaylistId, url, out string id);

        public bool IsValidVideo(string url, out string id)
            => IsValid(Globals.RegularExpression.YoutubeVideoId, url, out id);

        public bool IsValidVideo(string url)
            => IsValid(Globals.RegularExpression.YoutubeVideoId, url, out string id);



        public async Task<YoutubeResult> SearchAsync(string query, bool loadBasic = true, bool loadDetails = true)
        {
            var result = YoutubeResult.Invalid;
            //var list = new List<SongInfo>();
            if (!string.IsNullOrWhiteSpace(query))
            {
                if (IsValidPlaylist(query, out string playlistId))
                {
                    // Read playlist
                    result = await GetPlaylistByIdAsync(playlistId);

                }
                else if (IsValidVideo(query, out string videoId))
                {
                    // Return single
                    result = new YoutubeResult()
                    {
                        Id = videoId,
                        Type = YoutubeResultType.Video,
                        //SongsLoaded = true,
                        //Songs = new List<SongInfo>(new[] { await GetVideoByIdAsync(videoId).ConfigureAwait(false) })
                    };
                }
                else
                {
                    // Search manually
                    var songInfo = await GetVideoFromQueryAsync(query).ConfigureAwait(false);
                    result = new YoutubeResult()
                    {
                        Id = songInfo.Id,
                        Type = YoutubeResultType.Video,
                        SongsLoaded = true,
                        Songs = new List<SongInfo>(new[] { songInfo })
                    };
                }
            }
            if(loadBasic && result != YoutubeResult.Invalid)
            {
                return await result.LoadSongsAsync(this, loadDetails);
            }
            return result;
        }



        //===============================================================================================================
        // Video Methods
        //===============================================================================================================
        public async Task<SongInfo> GetVideoByIdAsync(string id)
        {
            var query = _service.Videos.List("snippet,contentDetails"); // liveStreamingDetails
            query.MaxResults = 1;
            query.Id = id;
            var item = (await query.ExecuteAsync())?.Items?.FirstOrDefault();
            if (item != null)
            {
                return new SongInfo()
                {
                    Id = item.Id,
                }
                .SetDetails(
                    item.Snippet?.Title ?? item.Snippet?.ChannelTitle,
                    item.Snippet?.LiveBroadcastContent?.ToLower() == "live",
                    XmlConvert.ToTimeSpan(item.ContentDetails.Duration)
                );
            }
            return SongInfo.Invalid;
        }

        public async Task<SongInfo> GetVideoFromQueryAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return await Task.FromResult(SongInfo.Invalid);

            // check whether we're searching for an existing youtube url
            var match = Globals.RegularExpression.YoutubeVideoId.Match(query);
            if (match.Success)
            {
                return await GetVideoByIdAsync(match.Groups["id"].Value).ConfigureAwait(false);
            }

            var querySearch = _service.Search.List("snippet");
            querySearch.MaxResults = 1;
            querySearch.Q = query;
            querySearch.Type = "video";
            var result = await querySearch.ExecuteAsync();

            //return result?.Items.Select(i => "http://www.youtube.com/watch?v=" + i.Id.VideoId).FirstOrDefault();
            var item = result?.Items?.FirstOrDefault();
            return new SongInfo()
            {
                Title = item.Snippet?.Title ?? item.Snippet?.ChannelTitle,
                Id = item.Id.VideoId,
                LiveStream = item.Snippet.LiveBroadcastContent?.ToLower() == "live",
            };
        }
            

            


        //===============================================================================================================
        // Playlist Methods
        //===============================================================================================================
        public async Task<YoutubeResult> GetPlaylistByIdAsync(string id)
        {
            var playlistQuery = _service.Playlists.List("snippet,contentDetails");
            playlistQuery.MaxResults = 1;
            playlistQuery.Id = id;
            var playlistResponse = await playlistQuery.ExecuteAsync();
            if (playlistResponse?.Items != null)
            {
                var item = playlistResponse.Items.FirstOrDefault();
                var totalSongs = item.ContentDetails.ItemCount;
                return new YoutubeResult()
                {
                    Id = id,
                    Type = YoutubeResultType.Playlist,
                    PlaylistInfo = new PlaylistInfo()
                    {
                        SongCount = (int)(item.ContentDetails.ItemCount ?? 0),
                        Title = item.Snippet.Title
                    }
                };
            }
            return YoutubeResult.Invalid;
        }

        public async Task<string> GetPlaylistNameAsync(string id)
        {
            var query = _service.Playlists.List("snippet");
            query.MaxResults = 1;
            query.Id = id;
            return (await query.ExecuteAsync())?.Items?.FirstOrDefault()?.Snippet?.Title;
        }

        /// <summary>
        /// Reads the songs from an unread playlist, returns the next page token.
        /// </summary>
        public async Task<string> ReadPlaylistSongsBasicAsync(YoutubeResult youtubeResult, string token = "")
        {
            if (!youtubeResult.SongsLoaded)
            {
                var playlistQuery = _service.PlaylistItems.List("snippet");
                playlistQuery.PlaylistId = youtubeResult.Id;
                playlistQuery.MaxResults = 50;
                playlistQuery.PageToken = token ?? "";

                var playlistResponse = await playlistQuery.ExecuteAsync(youtubeResult.CancellationToken);
                if (playlistResponse != null)
                {
                    youtubeResult.Songs.AddRange(
                        playlistResponse.Items.Select((e) => new SongInfo()
                        {
                            Id = e.Snippet.ResourceId.VideoId,
                            Title = e.Snippet.Title,
                            Duration = null
                        }
                    ).ToList());
                }
                return playlistResponse.NextPageToken;
            }
            return null;
        }

        public async Task ReadPlaylistSongDetailsAsync(YoutubeResult youtubeResult)
        {
            if (!youtubeResult.SongDetailsLoaded)
            {
                foreach (var songs in youtubeResult.Songs.ChunkBy(50))
                {
                    var queryVideo = _service.Videos.List("contentDetails,snippet"); // contentDetails for time, snippet for title
                    queryVideo.MaxResults = 50;
                    queryVideo.Id = string.Join(',', songs.Select(a => a.Id));
                    var videos = (await queryVideo.ExecuteAsync(youtubeResult.CancellationToken)).Items;
                    foreach (var video in videos)
                    {
                        var songIndex = youtubeResult.Songs.FindIndex(e => e.Id == video.Id);
                        if (songIndex >= 0)
                        {
                            var song = youtubeResult.Songs[songIndex];
                            //song.Title = video.Snippet.Title;
                            //song.Duration = XmlConvert.ToTimeSpan(video.ContentDetails.Duration) // ISO 8601 time format
                            song.SetDetails(
                                video.Snippet.Title,
                                video.Snippet.LiveBroadcastContent?.ToLower() == "live",
                                XmlConvert.ToTimeSpan(video.ContentDetails.Duration) // ISO 8601 time format
                            );
                            youtubeResult.Songs[songIndex] = song;
                        }
                    }
                }
            }
        }
    }
}
