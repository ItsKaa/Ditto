using Discord;
using System.Threading.Tasks;

namespace Ditto.Extensions
{
    public static class IMessageChannelExtensions
    {
        public static Task<IUserMessage> EmbedAsync(this IMessageChannel channel, EmbedBuilder embedBuilder, string message = "", RequestOptions options = null)
            => channel.SendMessageAsync(message,
                embed: embedBuilder.Build(),
                options: options
            );
    }
}
