using Discord;
using global::Discord.Commands;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ditto.Extensions
{
    public static class ICommandContextExtensions
    {
        public static Task<int> SendOptionDialogueAsync(this ICommandContext context,
            string headerMessage,
            IEnumerable<string> options,
            bool awaitSingleReaction,
            IEnumerable<IEmote> reactionEmotes = null,
            Action<int, string> onReaction = null,
            int timeout = 30000,
            int addedTimeoutOnReaction = 30000
            )
            => context.Channel.SendOptionDialogueAsync(headerMessage, options, context.Client, awaitSingleReaction, reactionEmotes, onReaction, timeout, addedTimeoutOnReaction);

        public static Task SendListDialogue(this ICommandContext context, string headerMessage, IEnumerable<string> options, int timeout = 30000)
            => context.Channel.SendListDialogue(headerMessage, options, context.Client, timeout);


        public static Task SendPagedMessageAsync(this ICommandContext context,
            EmbedBuilder embedBuilder,
            Func<IUserMessage, int, EmbedBuilder> onPageChange, // message, page
            int pageCount = int.MaxValue,
            bool displayPage = true,
            int timeout = 30000,
            int addedTimeoutOnReaction = 30000)
        => context.Channel.SendPagedMessageAsync(context.Client, embedBuilder, onPageChange, pageCount, displayPage, timeout, addedTimeoutOnReaction);

    }
}
