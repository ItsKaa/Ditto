using Ditto.Bot.Database.Data;
using System;

namespace Ditto.Bot.Database.Models
{
    public sealed class PlaylistSong : DbEntity
    {
        public int PlaylistId { get; set; }
        public string Creator { get; set; }
        public string Name { get; set; }
        public SongType Type { get; set; }
        public TimeSpan? Length { get; set; }
        public string Data { get; set; }
    }
}
