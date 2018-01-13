using Discord;
using Ditto.Bot.Database.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ditto.Bot.Database.Models
{
    public sealed class Playlist : DbEntity
    {
        public ulong? GuildId { get; set; }
        public string Creator { get; set; }
        public string Name { get; set; }
        public PlaylistType Type { get; set; }
        public string Data { get; set; }
        bool Enabled { get; set; }
        public DateTime DateAdded { get; set; }

        [NotMapped]
        public IGuild Guild
        {
            get => GetGuild(GuildId);
            set => GuildId = GetIdOf<ulong?>(value);
        }
        public List<PlaylistSong> Songs { get; set; } = new List<PlaylistSong>();
    }
}
