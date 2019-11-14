using Ditto.Data;
using System;

namespace Ditto.Bot.Data.API.Rest
{
    public class DadJokeApi : RestBase<DadJokeApi.Result>
    {
        public class Result
        {
            public string Id { get; set; }
            public string Joke { get; set; }
        }

        public DadJokeApi()
        {
            BaseUrl = new Uri("https://icanhazdadjoke.com/");
        }
        public string Joke()
        {
            return Call("", null)?.Joke;
        }

    }
}
