using Ditto.Attributes;
using System;

namespace Ditto.Bot.Data.Configuration
{
    [Serializable]
    public class CacheConfiguration
    {
        [Comment("\n  Sets the amount of seconds the internal cache handler should store values.\n")]
        public double CacheTime { get; set; } = 300;

        [Comment("\n  Sets the number of messages per channel that should be kept in cache. Setting this to zero disables the message cache entirely.\n")]
        public int AmountOfCachedMessages { get; set; } = 10;

        [Comment("\n  Sets the size of a single audio chunk, in bytes read from the remote audio stream. This value cannot exceed {0}. \n", ushort.MaxValue)]
        public ushort AudioBufferSize { get; set; } = 1024;

        [Comment("\n  Sets the total amount of memory, in megabytes the audio buffer can utilize. \n")]
        public uint AudioBufferLimit { get; set; } = 50;
    }
}
