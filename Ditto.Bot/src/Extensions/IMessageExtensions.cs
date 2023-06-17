using Discord;
using Discord.WebSocket;
using Ditto.Data;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ditto.Bot.Extensions
{
    public static class IMessageExtensions
    {
        public static async Task<Response<string>> AskQuestionAsync(
            this IMessageChannel channel,
            string question,
            IUser user,
            TimeSpan delay,
            bool deleteQuestionOnResponseOrError = true,
            Func<string, Task<bool>> onResponse = null)
        {
            var response = new Response<string>();
            var message = await channel.SendMessageAsync(question).ConfigureAwait(false);
            var cancellationSource = new CancellationTokenSource(TimeSpan.FromMinutes(5));

            // Create our function that will be hooked to the MessageReceived event, this will listen our response.
            var func = new Func<SocketMessage, Task>(m =>
            {
                if (m is SocketUserMessage msg)
                {
                    if (!response.Success && !msg.Author.IsBot && msg.Channel?.Id == channel.Id)
                    {
                        if (user == null || (user != null && user.Id == msg.Author.Id))
                        {
                            response.Success = true;
                            response.Result = msg.Content;
                        }
                    }
                }
                return Task.CompletedTask;
            });
            // Hook our function to the MessageReceived event.
            Ditto.Client.MessageReceived += func;

            // Await our response
            while (!response.Success && cancellationSource?.IsCancellationRequested != true)
            {
                await Task.Delay(100).ConfigureAwait(false);
            }

            // Trigger function
            if(response.Success && onResponse != null && !cancellationSource.IsCancellationRequested)
            {
                response.Success = await onResponse(response.Result).ConfigureAwait(false);
            }

            // Remove our event function
            Ditto.Client.MessageReceived -= func;

            // Return our response.
            if (deleteQuestionOnResponseOrError)
            {
                await message.DeleteAsync(new RequestOptions() { RetryMode = RetryMode.AlwaysRetry });
            }
            return response;
        }

        public static Task<Response<string>> AskQuestionAsync(
            this IMessageChannel channel,
            string question,
            TimeSpan delay,
            bool deleteQuestionOnResponseOrError = true,
            Func<string, Task<bool>> onResponse = null)
        {
            return AskQuestionAsync(channel, question, null, delay, deleteQuestionOnResponseOrError, onResponse);
        }
    }
}
