using Discord;
using System;
using System.Collections.Generic;

namespace Ditto.Data.Database
{
    public interface IBaseDbEntity
    {
        string GetAliases(IEnumerable<string> list);
        List<string> GetAliases(string @string);

        IGuild GetGuild(ulong? guildId);
        IChannel GetChannel(ulong? channelId);
        IUser GetUser(ulong? userId);
        IRole GetRole(ulong? guildId, ulong? roleId);
        T GetIdOf<T>(ISnowflakeEntity snowflakeEntity);

    }
}
