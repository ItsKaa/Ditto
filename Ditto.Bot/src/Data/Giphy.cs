using Ditto.Data.Exceptions;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Ditto.Bot.Data
{
    public struct GiphyResult
    {
        public string Type { get; set; } // type
        public string Id { get; set; } // id
        public string Url { get; set; } // url
        public Giphy.Rating Rating { get; set; } // rating
        public string SourceSite { get; set; } // source
        public string SourceUrl { get; set; } // source_post_url
        public DateTime ImportTime { get; set; } // import_datetime
        public DateTime TrendingTime { get; set; } // trending_datetime

        public string ShortDirectUrl { get; set; }
        public string DirectUrl => $"https://media.giphy.com/media/{Id}/giphy.gif";
        
        public static GiphyResult Empty { get; } = new GiphyResult()
        {
            Id = "",
            Type = "",
            Url = "",
            Rating = Giphy.Rating.Any,
            SourceSite = "",
            SourceUrl = "",
            ImportTime = DateTime.MinValue,
            TrendingTime = DateTime.MinValue,
            ShortDirectUrl = null,
        };

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            var hashCode = 1156222695;
            hashCode = hashCode * -1521134295 + base.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Type);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Id);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Url);
            hashCode = hashCode * -1521134295 + Rating.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(SourceSite);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(SourceUrl);
            hashCode = hashCode * -1521134295 + ImportTime.GetHashCode();
            hashCode = hashCode * -1521134295 + TrendingTime.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ShortDirectUrl);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(DirectUrl);
            return hashCode;
        }

        public static bool operator==(GiphyResult left, GiphyResult right)
        {
            return left.Equals(right);
        }
        public static bool operator !=(GiphyResult left, GiphyResult right)
        {
            return !(left == right);
        }

    }

    public class Giphy
    {
        public enum Rating : int
        {
            Any,
            Y,
            G,
            Pg,
            Pg13,
            R,
            Unrated,
            Nsfw
        }
        
        public string ApiKey { get; private set; }
        public string BaseUrl { get; private set; }
        
        public Giphy(string apiKey, string baseUrl = @"https://api.giphy.com/v1")
        {
            ApiKey = apiKey;
            BaseUrl = baseUrl;


            // Test connection
            try
            {
                var value = RandomAsync("cat").GetAwaiter().GetResult();
                if (value == GiphyResult.Empty)
                {
                    throw new ApiException(nameof(Giphy));
                }
            }
            catch { }
        }

        private IEnumerable<GiphyResult> ParseJson(JObject jsonObject)
        {
            var list = new List<GiphyResult>();
            var data = jsonObject["data"].Children(); // random = JEnumerable<JToken> ?

            var properties = data.OfType<JProperty>();
            if (properties.Count() > 0)
            {
                //var id = data["id"].Value<string>();
                //var url = data["url"].Value<string>();
                var type = properties.Single(e => e.Name == "type");
                var typeValue = (string)type.Value;
                list.Add(new GiphyResult()
                {
                    Type = (string)properties.Single(e => e.Name == "type").Value,
                    Id = (string)properties.Single(e => e.Name == "id").Value, //data["id"].Value<string>(),
                    Url = (string)properties.Single(e => e.Name == "url").Value, //data["url"].Value<string>(),
                    //VideoUrl = (string)properties.Single(e => e.Name == "image_mp4_url").Value,
                    //Width = (int)properties.Single(e => e.Name == "image_width").Value,//image_width
                    //Height = (int)properties.Single(e => e.Name == "image_height").Value, //image_height
                });
            }
            else
            {

                foreach (var obj in data)
                {
                    Rating rating = Rating.Any;
                    Enum.TryParse(obj.Value<string>("rating"), true, out rating);
                    list.Add(new GiphyResult()
                    {
                        Type = obj.Value<string>("type"),
                        Id = obj.Value<string>("id"),
                        Url = obj.Value<string>("url"),
                        Rating = rating,
                        SourceSite = obj.Value<string>("source"),
                        SourceUrl = obj.Value<string>("source_post_url"),
                        ImportTime = obj.Value<DateTime>("import_datetime"),
                        TrendingTime = obj.Value<DateTime>("trending_datetime"),
                        ShortDirectUrl = obj.Value<string>("bitly_gif_url"),
                    });
                }
            }
            return list;
        }

        private async Task<string> ReadAsync(string type, string arguments)
        {
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync($"{BaseUrl}/{type}?api_key={ApiKey}&{arguments}");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
            }
            return null;
        }

        public async Task<GiphyResult> RandomAsync(string query)
        {
            var content = await ReadAsync("gifs/random", $"tag={WebUtility.UrlEncode(query)}");
            if(content != null)
            {
                var item = ParseJson(JObject.Parse(content)).FirstOrDefault();
                if (item != GiphyResult.Empty)
                {
                    return await GetById(item.Id).ConfigureAwait(false);
                }
            }
            return GiphyResult.Empty;
        }

        public async Task<IEnumerable<GiphyResult>> SearchAsync(string query, int limit = 25, int offset = 0, Rating rating = Rating.Any)
        {
            var content = await ReadAsync("gifs/search", $"q={WebUtility.UrlEncode(query)}&limit={limit}&offset={offset}&fmt=json" +
                $"{(rating == Rating.Any ? "" : "&rating=" + Enum.GetName(typeof(Rating), rating).ToLower())}"
            );
            if(content != null)
            {
                return ParseJson(JObject.Parse(content));
            }
            return Enumerable.Empty<GiphyResult>();
        }

        public async Task<IEnumerable<GiphyResult>> TrendingAsync(int limit = 25, int offset = 0, Rating rating = Rating.Any)
        {
            var content = await ReadAsync("gifs/trending", $"limit={limit}&offset={offset}&fmt=json" +
                $"{(rating == Rating.Any ? "" : "&rating=" + Enum.GetName(typeof(Rating), rating).ToLower())}"
            );
            if (content != null)
            {
                return ParseJson(JObject.Parse(content));
            }
            return Enumerable.Empty<GiphyResult>();
        }

        public async Task<GiphyResult> GetById(string id)
        {
            var content = await ReadAsync("gifs", $"ids={id}");
            if(content != null)
            {
                return ParseJson(JObject.Parse(content)).FirstOrDefault();
            }
            return GiphyResult.Empty;
        }
    }
}
