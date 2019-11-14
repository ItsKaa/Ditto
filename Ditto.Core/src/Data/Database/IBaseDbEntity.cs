using Discord;
using System;
using System.Collections.Generic;

namespace Ditto.Data.Database
{
    public interface IBaseDbEntity
    {
        //string GetAliases(IEnumerable<string> list);
        //List<string> GetAliases(string @string);
        string GetStringFromList(IEnumerable<string> list, string seperator = ";");
        List<string> GetListFromString(string value, string seperator = ";");

        IGuild GetGuild(ulong? guildId);
        IChannel GetChannel(ulong? channelId);
        IMessage GetMessage(ulong? channelId, ulong? messageId);
        IUser GetUser(ulong? userId);
        IRole GetRole(ulong? guildId, ulong? roleId);
        T GetIdOf<T>(ISnowflakeEntity snowflakeEntity);

    }
}
