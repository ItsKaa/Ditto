using Ditto.Data;
using System;

namespace Ditto.Bot.Data.API.Rest
{
    public class TheCatApi : RestBase<TheCatApi.Result>
    {
        public class Result
        {
            public Uri Url { get; set; }
        }
        public TheCatApi()
        {
            BaseUrl = new Uri("http://thecatapi.com");
        }

        public Uri Random()
        {
            var value = Call("/api/images/get", new[] {
                new RestSharp.Parameter("format", "xml", RestSharp.ParameterType.GetOrPost),
                new RestSharp.Parameter("size", "full", RestSharp.ParameterType.GetOrPost)
            });
            return value.Url;
        }
    }
}
