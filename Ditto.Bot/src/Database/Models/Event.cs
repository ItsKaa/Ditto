using Discord;
using Ditto.Data;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ditto.Bot.Database.Models
{
    public sealed class Event : DbEntity, IEquatable<Event>
    {
        public ulong? GuildId { get; set; }
        public ulong? ChannelId { get; set; }
        public ulong? CreatorId { get; set; }
        public string CreatorName { get; set; }
        public TimeSpan TimeBegin { get; set; }
        public TimeSpan? TimeEnd { get; set; }
        public TimeSpan? TimeCountdown { get; set; }
        public TimeSpan? TimeOffset { get; set; }
        public Day Days { get; set; }
        public string Title { get; set; }
        public string MessageBody { get; set; }
        public string MessageHeader { get; set; }
        public string MessageFooter { get; set; }
        public DateTime? LastRun { get; set; }

        public bool Equals(Event other)
        {
            if (other == null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return GuildId == other.GuildId
                && ChannelId == other.ChannelId
                && CreatorId == other.CreatorId
                //&& CreatorName == other.CreatorName
                && TimeBegin == other.TimeBegin
                && TimeEnd == other.TimeEnd
                && TimeCountdown == other.TimeCountdown
                && TimeOffset == other.TimeOffset
                && Days == other.Days
                && string.Equals(Title, other.Title)
                && string.Equals(MessageBody, other.MessageBody)
                && string.Equals(MessageHeader, other.MessageHeader)
                && string.Equals(MessageFooter, other.MessageFooter)
                //&& LastRun == other.LastRun
                ;
        }

        [NotMapped]
        public IGuild Guild
        {
            get => GetGuild(GuildId);
            set => GuildId = GetIdOf<ulong?>(value);
        }

        [NotMapped]
        public IChannel Channel
        {
            get => GetChannel(ChannelId);
            set => ChannelId = GetIdOf<ulong?>(value);
        }

        [NotMapped]
        public IUser Creator
        {
            get => GetUser(CreatorId);
            set => CreatorId = GetIdOf<ulong?>(value);
        }

        [NotMapped]
        public IGuildUser CreatorGuild
        {
            get => GetUserGuild(CreatorId);
            set => Creator = value;
        }
    }
}
