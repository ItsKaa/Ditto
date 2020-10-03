using Discord.Commands;
using Ditto.Data;
using Ditto.Extensions;
using System;
using System.ComponentModel;

namespace Ditto.Bot.Data.API.Rest
{
    public class RamMoeApi : RestBase<RamMoeApi.Result>
    {
        public enum Type
        {
            
            Cry,
            Cuddle,
            Hug,
            Kiss,
            Lewd,
            Lick,
            Nom,
            Nyan,
            Owo,
            Pat,
            Pout,
            Rem,
            Slap,
            Smug,
            Stare,
            Tickle,
            Triggered,
            [Description("nsfw-gtn")]
            Nsfw,
            Potato,
            Kermit,
        }

        public class Result
        {
            public string Path { get; set; }
            public string Id { get; set; }
            public string Type { get; set; }
            public bool Nsfw { get; set; }
        }

        public RamMoeApi()
        {
            BaseUrl = new Uri("https://rra.ram.moe");
        }

        public Result RandomImage(Type type, bool nsfw = false)
        {
            var typeString = (type.GetAttribute<DescriptionAttribute>()?.Description ?? type.ToString()).ToLower();

            var result = Call("i/r/", new[]
            {
                new RestSharp.Parameter("type", typeString, RestSharp.ParameterType.GetOrPost),
                new RestSharp.Parameter("nsfw", nsfw.ToString().ToLower(), RestSharp.ParameterType.GetOrPost),
            });

            // Absolute path.
            if (!string.IsNullOrEmpty(result?.Path))
            {
                result.Path = BaseUrl.Append(result.Path).ToString();
            }

            return result;
        }

    }
}
