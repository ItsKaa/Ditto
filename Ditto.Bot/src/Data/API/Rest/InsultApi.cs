using Ditto.Data;
using System;
using System.Collections.Generic;

namespace Ditto.Bot.Data.API.Rest
{
    public class InsultApi : RestBase<InsultApi.Result>
    {
        public class Result
        {
            public string Insult { get; set; }
        }

        public InsultApi()
        {
            BaseUrl = new Uri("https://insult.mattbas.org");
        }
        public Result Insult(string name = null)
        {
            var parameters = new List<Parameter>();
            if(name != null)
            {
                parameters.Add(new Parameter("who", name));
            }
            return Call("api/insult.json", parameters);
        }
    }
}
