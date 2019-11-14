using Discord;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ditto.Bot.Database.Models
{
    public sealed class Module : DbEntity
    {
        public ulong? GuildId { get; set; }
        public string Name { get; set; }

        [NotMapped]
        public List<string> Aliases = new List<string>();

        [Column(nameof(Aliases))]
        public string AliasesString
        {
            get { return GetStringFromList(Aliases); }
            set { Aliases = GetListFromString(value); }
        }

        [NotMapped]
        public IGuild Guild
        {
            get => GetGuild(GuildId);
            set => GuildId = GetIdOf<ulong?>(value);
        }
    }
}
