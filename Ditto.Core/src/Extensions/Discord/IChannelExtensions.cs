using Discord;

namespace Ditto.Extensions
{
    public static class IChannelExtensions
    {
        public static string Mention(this IChannel channel)
        {
            return $"<#{channel.Id}>";
        }
    }
}
