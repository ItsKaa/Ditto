using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Ditto
{
    internal static partial class Globals
    {
        public static readonly string AppDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public static class RegularExpression
        {
            public static readonly Regex DiscordTagUser = new Regex(@"<@!?(?<id>\d+)>", RegexOptions.Compiled);
            public static readonly Regex DiscordTagChannel = new Regex(@"<#(?<id>\d+)>", RegexOptions.Compiled);
            public static readonly Regex DiscordTagRole = new Regex(@"<@&(?<id>\d+)>", RegexOptions.Compiled);

            public static readonly Regex YoutubeVideoId = new Regex(@"(?:youtube\.com\/\S*(?:(?:\/e(?:mbed))?\/|watch\?(?:\S*?&?v\=))|youtu\.be\/)(?<id>[a-zA-Z0-9_-]{6,11})", RegexOptions.Compiled);
            public static readonly Regex YoutubePlaylistId = new Regex(@"(?:youtube\.com\/\S*(?:(?:\/e(?:mbed))?(?:\S*?&?list\=))|youtu\.be\/\S*?&?list\=)(?<id>[a-zA-Z0-9_-]{12,})", RegexOptions.Compiled);

            public static readonly Regex SqlUnderscore = new Regex(@"((?<=.)[A-Z][a-zA-Z]*)|((?<=[a-zA-Z])\d+)", RegexOptions.Compiled);

            private const string _remindMessagePart = @"(?:\s+)?(?:and)?(?:\s+)?(?:in)?(?:\s+)?(?:at)?(?:\s+)?(?:[+,&.]+)?(?:\s+)?";
            public static Regex RemindMessage = new Regex(
                  $@"^{_remindMessagePart}"
                + $@"(?:(?:(?<repeat>(?:\s+)?(?:every)?(?:repeat)?))?){_remindMessagePart}"
                + $@"(?:(?:(?<months>\d+)(?:\s+)?(?:mo(?:nth)?s?))?){_remindMessagePart}"
                + $@"(?:(?:(?<weeks>\d+)(?:\s+)?(?:w(?:ee)?k?s?))?){_remindMessagePart}"
                + $@"(?:(?:(?<days>\d+)(?:\s+)?(?:d(?:ay)?s?))?){_remindMessagePart}"
                + $@"(?:(?:(?<hours>\d+)(?:\s+)?(?:h(?:ou)?r?s?))?){_remindMessagePart}"
                + $@"(?:(?:(?<minutes>\d+)(?:\s+)?(?:m(?:in)?u?t?e?s?))?){_remindMessagePart}"
                //+ $@"(?:(?:(?<seconds>\d+)(?:\s+)?(?:s(?:ec)?(?:ond)?s?))?)",
                + $@"(?:(?:(?<seconds>\d+)(?:\s+)?(?:s(?:ec)?(?:ond)?s?))?){_remindMessagePart}"
                + $@"(?:\s+)?(?:to)?(?:\s+)?(?:[+,&.]+)?(?:\s+)?"
                + $@"(?:(?:(?<text>[\W\w]+)(?:\s+)?)?)"

                , RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);
        }

        public static class Strings
        {
            public const string YoutubeVideoUrl = "http://www.youtube.com/watch?v=";
        }
        public static class Cache
        {
            public static readonly TimeSpan DefaultCacheTime = TimeSpan.FromMinutes(5);
        }

        public static class Command
        {
            public static class Score
            {
                public const int DoubleMultiword = -25; // Prefer not to use this, it gets sloppy :/
                public const int Optional = -5; // Prefer not to use this, but no real biggie
                public const int ParseFail = -10;
                public const int ParseSuccess = +10;
            }
        }

        // converting unicode characters: https://r12a.github.io/apps/conversion/
        public static class Character
        {
            public const string HiddenSpace = "\uDB40\uDC20"; // Tag Space: U+E0020
            public const string Clock = "⏰";
        }
    }
}
