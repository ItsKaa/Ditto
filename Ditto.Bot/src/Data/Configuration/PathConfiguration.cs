using Ditto.Attributes;
using System;

namespace Ditto.Bot.Data.Configuration
{
    public class PathConfiguration
    {
        [Comment("Sets the base solution directory.")]
        public string BaseDir { get; set; } = $"{Environment.CurrentDirectory}/../";

        [Comment("Sets the scripts directory, extended from BaseDir, used to update the bot using GIT.")]
        public string ScriptDir { get; set; } = ".scripts";
    }
}
