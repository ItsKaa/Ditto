using Discord;
using Discord.Net;
using Ditto.Data.Discord;
using Ditto.Helpers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ditto.Extensions.Discord
{
    public static class IUserMessageExtensions
    {
        public static async Task AddReactionsAsync(this IUserMessage userMessage, CancellationToken cancellationToken, params Emotes[] emotes)
        {
            var errorCount = 0;
            for (int i = 0; i < emotes.Length;)
            {
                var emote = emotes[i];
                try
                {
                    await userMessage.AddReactionAsync(
                        EmotesHelper.GetEmoji(emote),
                        new RequestOptions()
                        {
                            CancelToken = cancellationToken,
                            RetryMode = RetryMode.AlwaysRetry
                        }
                    ).ConfigureAwait(false);
                    i++;
                    errorCount = 0;
                }
                catch (Exception ex)
                {
                    if (errorCount > 2)
                    {
                        Log.Warn($"AddReactionsAsync: Skipping {i}:{emote}");
                        i++;
                    }
                    else
                    {
                        if (!(ex is RateLimitedException))
                        {
                            Log.Error($"AddReactionsAsync, {i}:{emote} | {ex}");
                            errorCount++;
                        }
                    }
                }
            }
        }
        
        public static Task AddReactionsAsync(this IUserMessage userMessage, params Emotes[] emotes)
        {
            return AddReactionsAsync(userMessage, CancellationToken.None, emotes);
        }
    }
}
