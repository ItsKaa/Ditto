using Ditto.Bot.Modules.Music.Data;
using Ditto.Bot.Services.Data;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Ditto.Bot.Services
{
    public partial class GoogleService
    {
        public partial class YoutubeService
        {
            protected YouTubeService _youtubeService { get; set; }
			
#if TESTING
            protected VideoLibrary.YouTube _youTube { get; } = VideoLibrary.YouTube.Default;
#endif // DEBUG

            public virtual Task SetupAsync(BaseClientService.Initializer bcs)
            {
                _youtubeService = new YouTubeService(bcs);
                return Task.CompletedTask;
            }

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

            public string GetVideoId(string url)
            {
                IsValidVideo(url, out string id);
                return id;
            }

            public string GetPlaylistId(string url)
            {
                IsValidPlaylist(url, out string id);
                return id;
            }
            

            public async Task<YoutubeResult> GetVideoUrlAsync(string search)
            {
                if (string.IsNullOrWhiteSpace(search))
                    return await Task.FromResult<YoutubeResult>(null);

                // check whether we're searching for an existing youtube url
                var match = Globals.RegularExpression.YoutubeVideoId.Match(search);
                if (match.Success)
                {
                    return await GetVideoNameAsync(match.Groups["id"].Value).ConfigureAwait(false);
                }

                var query = _youtubeService.Search.List("snippet");
                query.MaxResults = 1;
                query.Q = search;
                query.Type = "video";
                var result = await query.ExecuteAsync();

                //return result?.Items.Select(i => "http://www.youtube.com/watch?v=" + i.Id.VideoId).FirstOrDefault();
                var item = result?.Items?.FirstOrDefault();
                return new YoutubeResult()
                {
                    Title = item.Snippet?.Title ?? item.Snippet?.ChannelTitle,
                    Url = Globals.Strings.YoutubeVideoUrl + item.Id.VideoId
                };
            }

            public async Task<YoutubeResult> GetVideoNameAsync(string id)
            {
                var query = _youtubeService.Videos.List("snippet");
                query.MaxResults = 1;
                query.Id = id;
                var item = (await query.ExecuteAsync())?.Items?.FirstOrDefault();
                return item == null ? null :
                    new YoutubeResult()
                    {
                        Title = item.Snippet?.Title ?? item.Snippet?.ChannelTitle,
                        Url = Globals.Strings.YoutubeVideoUrl + item.Id
                    };
            }

            public async Task<string> GetPlaylistNameAsync(string id)
            {
                var query = _youtubeService.Playlists.List("snippet");
                query.MaxResults = 1;
                query.Id = id;
                return (await query.ExecuteAsync())?.Items?.FirstOrDefault()?.Snippet?.Title;
            }
            
#if TESTING
            public async Task<VideoLibrary.YouTubeVideo> ParseVideoAsync(string url)
            {
                var videos = await _youTube.GetAllVideosAsync(url).ConfigureAwait(false);
                var video = videos
                    .Where(v => v.AudioBitrate < 256)
                    .OrderByDescending(v => v.AudioBitrate)
                    .FirstOrDefault();
                return video;
            }
#endif // DEBUG




            /// <summary>
            /// Retrieves the youtube playlist data, including the most basic song info (Id).
            /// This method will automatically start tasks to retrieve detailed song information, see the property DataRetrievalTasks.
            /// </summary>
            /// <param name="id">playlist id</param>
            /// <param name="songsAddedAction">Action that gets executed every time we add songs to the collection, up to a maximum of 50.</param>
            /// <param name="songsAddedCompletedAction">Action that gets executed after every song has been added.</param>
            /// <param name="songsDetailsLoaded"></param>
            /// <param name="cancellationToken"></param>
            /// <returns></returns>
            public Task<PlaylistInfo> GetPlaylistInfo(string id, Action<PlaylistInfo> songsAddedAction, Action<PlaylistInfo> songsAddedCompletedAction, Action<List<SongInfo>> songsDetailsLoaded, CancellationToken cancellationToken)
            {
                return Task.Run(async () =>
                {
                    PlaylistInfo playlistInfo = null;

                    // Get basic playlist info
                    var playlistQuery = _youtubeService.Playlists.List("snippet,contentDetails");
                    playlistQuery.MaxResults = 1;
                    playlistQuery.Id = id;
                    var playlistResponse = await playlistQuery.ExecuteAsync();
                    if (playlistResponse?.Items != null)
                    {
                        var item = playlistResponse.Items.FirstOrDefault();
                        playlistInfo = new PlaylistInfo()
                        {
                            Id = item.Id,
                            Title = item.Snippet.Title,
                            TotalSongs = item.ContentDetails.ItemCount ?? 0
                        };
                    }
                    else
                    {
                        // error #1: playlist not found
                        return null;
                    }

                    // Get playlist basic song info
                    PlaylistItemListResponse playlistItemResponse = null;
                    var token = "";
                    var current = 0;
                    while (token != null)
                    {
                        var playlistItemQuery = _youtubeService.PlaylistItems.List("snippet");
                        playlistItemQuery.PlaylistId = id;
                        playlistItemQuery.MaxResults = 50;
                        playlistItemQuery.PageToken = token;

                        playlistItemResponse = await playlistItemQuery.ExecuteAsync();
                        if (playlistItemResponse.Items != null)
                        {
                            var playlistItemSongs = playlistItemResponse.Items.Select((e)
                                => new SongInfo()
                                {
                                    Id = e.Snippet.ResourceId.VideoId,
                                    Title = e.Snippet.Title,
                                    Duration = null,
                                    DetailsLoaded = false
                                }
                            ).ToList();
                            playlistInfo.Songs.AddRange(playlistItemSongs);
                            songsAddedAction(playlistInfo);
                            playlistInfo.DataRetrievalTasks.Add(Task.Run(async () =>
                            {
                                var queryVideo = _youtubeService.Videos.List("contentDetails,snippet"); // contentDetails for time, snippet for title (maybe liveStreamingDetails??)
                                queryVideo.MaxResults = 50;
                                queryVideo.Id = string.Join(',', playlistItemSongs.Select(a => a.Id));
                                var videos = (await queryVideo.ExecuteAsync()).Items;
                                var songs = new List<SongInfo>();
                                foreach (var video in videos)
                                {
                                    var song = playlistItemSongs.FirstOrDefault(e => e.Id == video.Id);
                                    if (song != null)
                                    {
                                        song.Title = video.Snippet.Title;
                                        song.Duration = XmlConvert.ToTimeSpan(video.ContentDetails.Duration); // ISO 8601 time format
                                        song.DetailsLoaded = true;
                                        songs.Add(song);
                                    }
                                }
                                current += videos.Count;
                                songsDetailsLoaded(songs);
                                //Log.Info("{0}/{1}/{2}", current, playlistInfo.Songs.Count, playlistInfo.TotalSongs);
                            }, cancellationToken));

                            //playlistInfo.Songs.Add()
                            // Get video details
                            //var ids = string.Join(',', result.Items.Select(a => a.Snippet.ResourceId.VideoId));
                            //var queryVideo = _youtubeService.Videos.List("contentDetails,snippet"); // contentDetails for time, snippet for title (and liveStreamingDetails is obvious)
                            //queryVideo.MaxResults = 50;
                            //queryVideo.Id = ids;
                            //var videos = (await queryVideo.ExecuteAsync()).Items;
                            //list.AddRange(videos);
                        }
                        token = playlistItemResponse.NextPageToken;
                    }
                    songsAddedCompletedAction(playlistInfo);
                    return playlistInfo;
                }, cancellationToken);
            }

            public async Task<SongInfo> GetSongInfoAsync(string id)
            {
                var songQuery = _youtubeService.Videos.List("snippet,contentDetails,liveStreamingDetails");
                songQuery.MaxResults = 1;
                songQuery.Id = id;
                var songResponse = await songQuery.ExecuteAsync();
                if (songResponse?.Items != null)
                {
                    var item = songResponse.Items.FirstOrDefault();
                    return new SongInfo()
                    {
                        LiveStream = item.LiveStreamingDetails != null,
                        Id = item.Id,
                        Title = item.Snippet.Title,
                        Duration = XmlConvert.ToTimeSpan(item.ContentDetails.Duration), // ISO 8601 time format
                        DetailsLoaded = true
                    };
                }
                return null;
            }
        }
    }

}
