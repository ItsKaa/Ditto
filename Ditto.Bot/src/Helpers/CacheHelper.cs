using Discord;
using System.Runtime.CompilerServices;

namespace Ditto.Bot.Helpers
{
    public static class CacheHelper
    {
        public static string GetCacheName(ulong? id, [CallerMemberName] string propertyName = "") => $"{propertyName}{(id == null ? "" : "_")}{id}";
        public static string GetCacheName(IGuild guild, [CallerMemberName] string propertyName = "") => GetCacheName(guild?.Id, propertyName);
    }
}
