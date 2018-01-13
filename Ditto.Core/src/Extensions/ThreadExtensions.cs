using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ditto.Extensions
{
    public static class ThreadExtensions
    {
        public static void Do(this SemaphoreSlim slim, Action action, CancellationToken? token = null)
        {
            if (token.HasValue)
                slim.Wait(token.Value);
            else
                slim.Wait();
            try { action(); } catch { }
            slim.Release();
        }
        public static async Task<TReturn> DoAsync<TReturn>(this SemaphoreSlim slim, Func<Task<TReturn>> action, CancellationToken? token = null)
        {
            if (token.HasValue)
                await slim.WaitAsync(token.Value);
            else
                await slim.WaitAsync();
            TReturn result = default(TReturn);
            try { result = await action(); } catch { }
            slim.Release();
            return result;
        }
        public static async Task DoAsync(this SemaphoreSlim slim, Action action, CancellationToken? token = null)
        {
            if (token.HasValue)
                await slim.WaitAsync(token.Value);
            else
                await slim.WaitAsync();
            try { action(); } catch { }
            slim.Release();
        }
    }
}
