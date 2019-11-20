using Ditto.Attributes;
using System;

namespace Ditto.Bot.Data.Configuration
{
    [Serializable]
    public class PathConfiguration
    {
        [Comment("Sets the base solution directory.")]
        public string BaseDir { get; set; } = $"{Environment.CurrentDirectory}/../";

        [Comment("Sets the scripts directory, extended from BaseDir, used to update the bot using GIT.")]
        public string ScriptDir { get; set; } = ".scripts";

        [Comment("Sets the Youtube-DL path, this should be modified in case it is not added to your PATH variable.")]
        public string YoutubeDL { get; set; } = "youtube-dl";

        [Comment("Sets the FFmpeg path, this should be modified in case it is not added to your PATH variable.")]
        public string FFmpeg { get; set; } = "ffmpeg";
    }
}
