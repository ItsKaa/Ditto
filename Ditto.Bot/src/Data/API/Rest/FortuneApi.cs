using Ditto.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ditto.Bot.Data.API.Rest
{
    // http://fortunecookieapi.herokuapp.com
    public class FortuneCookieApi : RestBase<List<FortuneCookieApi.Result>>
    {
        public class Result
        {
            public string Message { get; set; }
            public string Id { get; set; }
        }
        
        public FortuneCookieApi()
        {
            BaseUrl = new Uri("http://fortunecookieapi.herokuapp.com");
        }

        public string Fortune()
        {
            var fortune = Call("v1/fortunes", new[] {
                new RestSharp.Parameter() {Name = "limit", Value = "1"},
                new RestSharp.Parameter() {Name = "skip", Value = Randomizer.New(0, 543)} // 2018-01-26: limit of 544 results.
            });
            return fortune.FirstOrDefault()?.Message ?? string.Empty;
        }
    }
}
