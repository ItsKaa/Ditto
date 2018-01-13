using Discord;
using Ditto.Bot.Database.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ditto.Bot.Database.Models
{
    public class Link : DbEntity
    {
        public LinkType Type { get; set; }
        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }
        public string Value { get; set; }
        public DateTime Date { get; set; }

        [NotMapped]
        public List<LinkItem> Links { get; set; } = new List<LinkItem>();

        [NotMapped]
        public IGuild Guild
        {
            get => GetGuild(GuildId);
            set => GuildId = GetIdOf<ulong>(value);
        }

        [NotMapped]
        public ITextChannel Channel
        {
            get => GetChannel(ChannelId) as ITextChannel;
            set => ChannelId = GetIdOf<ulong>(value);
        }
    }
}
