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
        }
    }

}
