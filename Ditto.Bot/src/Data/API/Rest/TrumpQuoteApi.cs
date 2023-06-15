using Ditto.Data;
using System;

namespace Ditto.Bot.Data.API.Rest
{
    public class TrumpQuoteApi : RestBase<TrumpQuoteApi.Result>
    {
        public class Result
        {
            public string Message { get; set; }
        }
        public TrumpQuoteApi()
        {
            BaseUrl = new Uri("https://api.whatdoestrumpthink.com/api/");
        }

        public string Quote(string name = null)
        {
            return Call(name == null ? "v1/quotes/random" : "v1/quotes/personalized", name == null ? null : new[] {
                new Parameter("q", name)
            })?.Message ?? "";
        }
    }
}
