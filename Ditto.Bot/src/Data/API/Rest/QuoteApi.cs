using Ditto.Data;
using System;

namespace Ditto.Bot.Data.API.Rest
{
    public class QuoteApi : RestBase<QuoteApi.Result>
    {
        public class Result
        {
            public string QuoteText { get; set; }
            public string QuoteAuthor { get; set; }
        }

        public QuoteApi()
        {
            BaseUrl = new Uri("https://api.forismatic.com");
        }
        
        public QuoteApi.Result Quote()
        {
            return Call("api/1.0/", new[]
            {
                new Parameter("method", "getQuote"),
                new Parameter("lang", "en"),
                new Parameter("format", "json"),
            });
        }
    }
}
