using Discord;
using Ditto.Data.Database;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ditto.Bot.Database
{
    /// <summary>
    /// Entity with no defined keys
    /// </summary>
    public abstract class BaseDbEntity : IBaseDbEntity
    {
        public string GetAliases(IEnumerable<string> list)
        {
            if (list == null)
                return null;
            return string.Join(';', list);
        }
        public List<string> GetAliases(string @string)
        {
            if (@string == null)
                return new List<string>();
            return @string.Split(';').ToList();
        }

        public IGuild GetGuild(ulong? guildId)
        => guildId.HasValue ? Ditto.Client.Do((c) => c?.GetGuild(guildId.Value)) : null;

        public IChannel GetChannel(ulong? channelId)
            => channelId.HasValue ? Ditto.Client.Do((c) => c?.GetChannel(channelId.Value)) : null;

        public IUser GetUser(ulong? userId)
            => userId.HasValue ? Ditto.Client.Do((c) => c?.GetUser(userId.Value)) : null;
        
        public IRole GetRole(ulong? guildId, ulong? roleId)
            => roleId.HasValue ? GetGuild(guildId)?.GetRole(roleId.Value) : null;

        public T GetIdOf<T>(ISnowflakeEntity snowflakeEntity)
        {
            Type type = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
            //if (Nullable.GetUnderlyingType(typeof(T)) != null)
            //{
            //    if (snowflakeEntity == null)
            //        return default(T);
            //    return (T)Convert.ChangeType(snowflakeEntity.Id, typeof(T));
            //}
            //return (T)Convert.ChangeType(snowflakeEntity?.Id ?? default(ulong), typeof(T));
            return (snowflakeEntity == null ? default(T)
                : (T)Convert.ChangeType(snowflakeEntity.Id, type)
            );
        }
    }
}
//