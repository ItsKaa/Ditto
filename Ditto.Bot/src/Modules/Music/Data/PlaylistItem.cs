using Discord;
using Ditto.Bot.Services.Data;
using System;

namespace Ditto.Bot.Modules.Music.Data
{
    public struct PlaylistItem : IEquatable<PlaylistItem>
    {
        public IGuildUser GuildUser { get; set; }
        public SongInfo Song { get; set; }

        public bool Equals(PlaylistItem other)
        {
            return
                GuildUser?.Id == other.GuildUser?.Id
                && Song.Equals(other.Song);
        }
    }
}
