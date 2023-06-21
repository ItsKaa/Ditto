using Ditto.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ditto.Bot.Data.API.Rest
{
    public class TheCatApi : RestBase<List<TheCatApi.Result>>
    {
        public class Result
        {
            public Uri Url { get; set; }
        }
        public TheCatApi()
        {
            BaseUrl = new Uri("http://api.thecatapi.com");
        }

        public Uri Random()
        {
            var value = Call("/v1/images/search", new[] {
                new Parameter("format", "xml"),
                new Parameter("size", "full")
            });
            return value.FirstOrDefault()?.Url ?? new Uri(string.Empty);
        }
    }
}
