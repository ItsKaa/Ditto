using Ditto.Helpers;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Ditto.Bot.Data
{
    public static class UrbanDictionary
    {
        public enum ResultType
        {
            None,
            Exact,
        }
        public struct Definition
        {
            public string Id { get; set; }
            public string Value { get; set; }
            public string Word { get; set; }
            public string Example { get; set; }
            public string Link { get; set; }
            public string Author { get; set; }
            public int ThumbsUp { get; set; }
            public int ThumbsDown { get; set; }
        }
        public class Result
        {
            public ResultType Type { get; set; }
            public IEnumerable<string> Tags { get; set; }
            public IEnumerable<Definition> Definitions { get; set; }
            public IEnumerable<string> SoundUrls { get; set; }
        }
        
        public static async Task<Result> Define(string query)
        {
            var htmlCode = await WebHelper.GetSourceCodeAsync($"http://api.urbandictionary.com/v0/define?term={WebUtility.UrlEncode(query)}").ConfigureAwait(false);
            if (htmlCode != null)
            {
                try
                {
                    var result = new Result();
                    var json = JObject.Parse(htmlCode);
                    if (Enum.TryParse(json["result_type"]?.Value<string>(), true, out ResultType type))
                    {
                        result.Type = type;
                    }

                    result.Tags = json["tags"]?.Values<string>() ?? Enumerable.Empty<string>();
                    result.Definitions = (json["list"]?.Children() ?? JEnumerable<JToken>.Empty)
                        .Select(e => new Definition()
                        {
                            Id = e["defid"]?.Value<string>(),
                            Value = e["definition"]?.Value<string>(),
                            Author = e["author"]?.Value<string>(),
                            Word = e["word"]?.Value<string>()?.ToLower(),
                            Example = e["example"]?.Value<string>(),
                            Link = e["permalink"]?.Value<string>(),
                            ThumbsUp = e["thumbs_up"]?.Value<int?>() ?? 0,
                            ThumbsDown = e["thumbs_down"]?.Value<int?>() ?? 0,
                        }
                    ).Where(e => e.Value != null);

                    result.SoundUrls = (json["sounds"]?.Children())?.Values<string>() ?? Enumerable.Empty<string>();
                    return result;
                }
                catch { }
            }
            return null;
        }
    }
}
