using Discord;
using Ditto.Attributes;
using Ditto.Data.Discord;
using Ditto.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ditto.Helpers
{
    public static class EmotesHelper
    {
        public static IReadOnlyCollection<Emotes> Emotes { get; private set; }
        public static IReadOnlyCollection<Emoji>  Emojis { get; private set; }

        static EmotesHelper()
        {
            Emotes = new List<Emotes>(Enum.GetValues(typeof(Emotes)).OfType<Emotes>()).AsReadOnly();
            Emojis = new List<Emoji>(Emotes.Select(e => GetEmoji(e))).AsReadOnly();
        }

        public static string GetString(Emotes emote)
        {
            var attribute = typeof(Emotes).GetMember(emote.ToString()).FirstOrDefault()?.GetCustomAttributes(typeof(EmoteValueAttribute), false).FirstOrDefault();
            if (attribute != null && attribute is EmoteValueAttribute emoteValueAttribute)
            {
                return emoteValueAttribute.Value;
            }
            return null;
        }

        public static Emoji GetEmojiFromString(string value)
        {
            value = value.Replace("🏻", "")
                    .Replace("🏼", "")
                    .Replace("🏽", "")
                    .Replace("🏾", "")
                    .Replace("🏿", "");
            return Emojis.FirstOrDefault(e => e.Name == value);
        }

        public static Emoji GetEmoji(Emotes emote)
        {
            return new Emoji(GetString(emote));
        }

        public static string GetEmojiName(Emotes emote)
        {
            return Globals.RegularExpression.SqlUnderscore.Replace(emote.ToString(), @"_$1$2").ToLower();
        }
        
        //https://discordapp.com/assets/0.1ae4f42f7d229659c4dd.js
        //public static async Task ReadFromWeb()
        //{
        //    var sourceCode = await Helpers.WebHelper.GetSourceCodeAsync("https://discordapp.com/assets/0.74f15ff11a53f81f31de.js");
        //    var json = "{people:" + sourceCode.Between(
        //        "{e.exports={people:",
        //        "}]}}"
        //    ) + "}]}";
        //    json = json.Replace("e.exports", "e_exports");
        //    json = json.Replace(",hasDiversity:!0", "");
        //    var jsonObject = JsonConvert.DeserializeObject<JObject>(json);
        //    var values = jsonObject.ToObject<Dictionary<string, List<EmojiJsonItem>>>();

        //    var enumString = "public enum Emotes\n{\n";
        //    foreach (var item in values)
        //    {
        //        enumString += "\t//" + new string('=', 50) + "\n"
        //            + $"\t// {item.Key.ToTitleCase()}\n"
        //            + "\t//" + new string('=', 50)
        //            ;
        //        foreach (var value in item.Value)
        //        {
        //            var name = value.Names.FirstOrDefault().Replace("_", " ").ToTitleCase().Replace(" ", "");
        //            if (char.IsNumber(name, 0))
        //            {
        //                name = $"_{name}";
        //            }
        //            enumString += $"\n\t[EmojiValue(\"{value.Surrogates}\")] {name},";
        //        }
        //        enumString += "\n\t\n";
        //    }
        //    enumString += "}";
        //    var x = 0;
        //}

        //public struct EmojiJsonItem
        //{
        //    public List<string> Names { get; set; }
        //    public string Surrogates { get; set; }
        //}
    }
}
