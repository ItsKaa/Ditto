using global::Discord.Commands;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ditto.Extensions
{
    public static class ICommandContextExtensions
    {
        public static Task<int> SendOptionDialogueAsync(this ICommandContext context, string headerMessage, string[] options, int timeout = 30000)
            => context.Channel.SendOptionDialogueAsync(headerMessage, options, context.Client, timeout);

        public static Task<int> SendOptionDialogueAsync(this ICommandContext context, string headerMessage, IEnumerable<string> options, int timeout = 30000)
            => context.Channel.SendOptionDialogueAsync(headerMessage, options, context.Client, timeout);

        public static Task SendListDialogue(this ICommandContext context, string headerMessage, string[] options, int timeout = 30000)
            => context.Channel.SendListDialogue(headerMessage, options, context.Client, timeout);

        public static Task SendListDialogue(this ICommandContext context, string headerMessage, IEnumerable<string> options, int timeout = 30000)
            => context.Channel.SendListDialogue(headerMessage, options, context.Client, timeout);
    }
}
