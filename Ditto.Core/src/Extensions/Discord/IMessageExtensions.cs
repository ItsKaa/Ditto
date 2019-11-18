using Discord;
using Discord.Net;
using Ditto.Data.Commands;
using Ditto.Data.Discord;
using Ditto.Helpers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ditto.Extensions.Discord
{
    public static class IMessageExtensions
    {
        public static Task DeleteAfterAsync(this IMessage message, TimeSpan? timer = null, RetryMode retryMode = RetryMode.AlwaysRetry, CancellationToken? cancellationToken = null)
        {
            var cancelToken = cancellationToken ?? CancellationToken.None;
            var _ = Task.Run(async() =>
            {
                await Task.Delay(timer ?? TimeSpan.Zero, cancelToken);
                await message.DeleteAsync(new RequestOptions() { RetryMode = retryMode });
            }, cancelToken);
            return Task.CompletedTask;
        }
        public static Task DeleteAfterAsync(this IMessage message, double seconds, RetryMode retryMode = RetryMode.AlwaysRetry, CancellationToken? cancellationToken = null)
            => DeleteAfterAsync(message, TimeSpan.FromSeconds(seconds), retryMode, cancellationToken);
        
        public static async Task DeleteAfterAsync(this Task<IUserMessage> messageTask, TimeSpan? timer = null, RetryMode retryMode = RetryMode.AlwaysRetry, CancellationToken? cancellationToken = null)
            => await DeleteAfterAsync(await messageTask.ConfigureAwait(false), timer, retryMode, cancellationToken).ConfigureAwait(false);

        public static Task DeleteAfterAsync(this Task<IUserMessage> messageTask, double seconds, RetryMode retryMode = RetryMode.AlwaysRetry, CancellationToken? cancellationToken = null)
            => DeleteAfterAsync(messageTask, TimeSpan.FromSeconds(seconds), retryMode, cancellationToken);

        public static async Task AddReactionsAsync(this IMessage message, CancellationToken cancellationToken, params Emotes[] emotes)
        {
            var errorCount = 0;
            for (int i = 0; i < emotes.Length;)
            {
                var emote = emotes[i];
                try
                {
                    await message.AddReactionAsync(
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

        public static Task AddReactionsAsync(this IMessage message, params Emotes[] emotes)
        {
            return AddReactionsAsync(message, CancellationToken.None, emotes);
        }

        /// <summary> Set a reaction to confirm the exeuction or failure of the used command. </summary>
        public static async Task SetResultAsync(this IMessage message, CommandResult commandResult)
        {
            Emotes? reaction = null;
            switch (commandResult)
            {
                case CommandResult.Success:
                    reaction = Emotes.WhiteCheckMark;
                    break;
                case CommandResult.SuccessAlt1:
                    reaction = Emotes.BallotBoxWithCheck;
                    break;
                case CommandResult.SuccessAlt2:
                    reaction = Emotes.HeavyCheckMark;
                    break;
                case CommandResult.Failed:
                    reaction = Emotes.X;
                    break;
                case CommandResult.InvalidParameters:
                    reaction = Emotes.Anger;
                    break;
                case CommandResult.FailedBotPermission:
                    reaction = Emotes.NoEntrySign;
                    break;
                case CommandResult.FailedUserPermission:
                    reaction = Emotes.NoEntry;
                    break;
            }

            if (reaction.HasValue)
            {
                await message.AddReactionsAsync(reaction.Value).ConfigureAwait(false);
            }
        }

    }
}
