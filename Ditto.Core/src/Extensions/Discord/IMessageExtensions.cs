using Discord;
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
    }
}
