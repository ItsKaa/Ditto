using Ditto.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace Ditto.Common
{

    public static class NumbersApi
    {
        public enum Type
        {
            Trivia,
            Math,
            Date,
            Year,
        }

        [Serializable]
        public class Result
        {
            public Type Type { get; set; }
            public string Text { get; set; }
            public ulong? Year { get; set; }
            public ulong Number { get; set; }
            public bool ExactMatch { get; set; }
        }

        public enum NotFoundOption
        {
            Default,
            Floor,
            Ceil
        }

        private static readonly string _baseUrl = "http://numbersapi.com";
        
        private static async Task<Result> ParseAsync(Type type, string value, NotFoundOption notFoundOption = NotFoundOption.Default, bool fragment = false)
        {
            var htmlCode = await WebHelper.GetSourceCodeAsync(
                $"{_baseUrl}/{value}/{type.ToString().ToLower()}"
                + $"?json"
                + $"&notfound={notFoundOption.ToString().ToLower()}"
                + $"&{(fragment ? "fragment" : "")}"
            ).ConfigureAwait(false);
            try
            {
                return JsonConvert.DeserializeObject<Result>(htmlCode);
            }
            catch { }
            return null;
        }


        public static Task<Result> Trivia(ulong number, NotFoundOption notFoundOption = NotFoundOption.Default, bool fragment = false)
        {
            return ParseAsync(Type.Trivia, number.ToString(), notFoundOption, fragment);
        }

        public static Task<Result> Math(ulong number, NotFoundOption notFoundOption = NotFoundOption.Default, bool fragment = false)
        {
            return ParseAsync(Type.Math, number.ToString(), notFoundOption, fragment);
        }

        public static Task<Result> Date(uint months, uint days, NotFoundOption notFoundOption = NotFoundOption.Default, bool fragment = false)
        {
            return ParseAsync(Type.Date, $"{months}/{days}", notFoundOption, fragment);
        }

        public static Task<Result> Year(int year, NotFoundOption notFoundOption = NotFoundOption.Default, bool fragment = false)
        {
            return ParseAsync(Type.Year, $"{year}", notFoundOption, fragment);
        }
    }
}

