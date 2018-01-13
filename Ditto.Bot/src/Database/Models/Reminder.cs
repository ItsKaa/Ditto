using Discord;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ditto.Bot.Database.Models
{
    public sealed class Reminder : DbEntity, IEquatable<Reminder>
    {
        public ulong? GuildId { get; set; }
        public ulong? ChannelId { get; set; }
        public ulong? UserId { get; set; }
        public ulong? RoleId { get; set; }
        public string Creator { get; set;}
        public string Message { get; set; }
        public bool Self { get; set; }
        public bool Repeat { get; set; }
        public DateTime StartTime { get; set; } = DateTime.Now;
        public DateTime EndTime { get; set; }

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
        public IUser User
        {
            get => GetUser(UserId);
            set => UserId = GetIdOf<ulong?>(value);
        }

        [NotMapped]
        public IRole Role
        {
            get => GetRole(GuildId, RoleId);
            set => RoleId = GetIdOf<ulong?>(value);
        }

        public bool Equals(Reminder other)
        {
            if (other == null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return (GuildId == other.GuildId
                && ChannelId == other.ChannelId
                && UserId == other.UserId
                //&& RoleId == other.RoleId
                && Creator == other.Creator
                && Message == other.Message
                && Repeat == other.Repeat
                && StartTime == other.StartTime
                && EndTime == other.EndTime);
        }
    }
}
