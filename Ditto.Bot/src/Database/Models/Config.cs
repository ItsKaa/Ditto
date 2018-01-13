using Discord;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ditto.Bot.Database.Models
{
    public sealed class Config : DbEntity
    {
        public ulong? GuildId { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }

        [NotMapped]
        public IGuild Guild
        {
            get => GetGuild(GuildId);
            set => GuildId = GetIdOf<ulong?>(value);
        }
    }
}
