using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ditto.Data
{
    public class ObjectLock<TValue> : IDisposable
        where TValue : class
    {
        public SemaphoreSlim SemaphoreSlim { get; private set; }
        private TValue Value { get; set; } = null;
        public bool HasValue => Value != null;
        
        public ObjectLock(TValue value, ushort initialCount = 1)
        {
            SemaphoreSlim = new SemaphoreSlim(initialCount);
            Value = value;
        }
        public ObjectLock(TValue value, ushort initialCount, ushort maxCount)
        {
            SemaphoreSlim = new SemaphoreSlim(initialCount, maxCount);
            Value = value;
        }
        public ObjectLock(ushort initialCount = 1) => SemaphoreSlim = new SemaphoreSlim(initialCount);
        public ObjectLock(ushort initialCount, ushort maxCount) => SemaphoreSlim = new SemaphoreSlim(initialCount, maxCount);

        public void Dispose()
        {
            try { SemaphoreSlim?.Dispose(); } catch { }
            if (Value is IDisposable disposableValue)
            {
                try { disposableValue?.Dispose(); } catch { }
            }
            //Value = null;
        }

        /// <summary>
        /// Blocks the current thread until it can enter the System.Threading.SemaphoreSlim and sets the value.
        /// </summary>
        public void Set(TValue value)
        {
            SemaphoreSlim.Wait();
            Value = value;
            SemaphoreSlim.Release();
        }

        /// <summary>
        /// Blocks the current thread until it can enter the System.Threading.SemaphoreSlim and sets the value.
        /// </summary>
        public async Task SetAsync(TValue @object)
        {
            await SemaphoreSlim.WaitAsync();
            Value = @object;
            SemaphoreSlim.Release();
        }

        /// <summary>
        /// Blocks the current thread until it can enter the System.Threading.SemaphoreSlim, while observing a System.Threading.CancellationToken.
        /// </summary>
        /// <param name="token">The System.Threading.CancellationToken token to observe.</param>
        public void Wait(CancellationToken? token = null)
        {
            if (token.HasValue)
                SemaphoreSlim.Wait(token.Value);
            else
                SemaphoreSlim.Wait();
        }

        /// <summary>
        /// Asynchronously waits to enter the System.Threading.SemaphoreSlim, while observing a System.Threading.CancellationToken.
        /// </summary>
        /// <param name="token">The System.Threading.CancellationToken token to observe.</param>
        public async Task WaitAsync(CancellationToken? token = null)
        {
            if (token.HasValue)
                await SemaphoreSlim.WaitAsync(token.Value);
            else
                await SemaphoreSlim.WaitAsync();
        }

        /// <summary>
        /// Releases the System.Threading.SemaphoreSlim object once.
        /// </summary>
        /// <returns>The previous count of the System.Threading.SemaphoreSlim.</returns>
        public int Release() => SemaphoreSlim.Release();

        /// <summary>
        /// Releases the System.Threading.SemaphoreSlim object a specified number of times.
        /// </summary>
        /// <param name="releaseCount">The number of times to exit the semaphore.</param>
        /// <returns>The previous count of the System.Threading.SemaphoreSlim.</returns>
        public int Release(int releaseCount) => SemaphoreSlim.Release(releaseCount);

        /// <summary>
        /// Blocks the current thread until it can enter the SemaphoreSlim, executes the action and then releases the SemaphoreSlim.
        /// </summary>
        public void Do(Action<TValue> action, CancellationToken? token = null)
        {
            try
            {
                Wait(token);
                action(Value);
            }
            //catch (OperationCanceledException) { }
            catch (Exception)
            {
                throw;
            }
            Release();
        }

        /// <summary>
        /// Blocks the current thread until it can enter the SemaphoreSlim, executes the action and then releases the SemaphoreSlim.
        /// </summary>
        public TResult Do<TResult>(Func<TValue, TResult> func, CancellationToken? token = null)
        {
            try
            {
                Wait(token);
                return func(Value);
            }
            //catch (OperationCanceledException) { }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                Release();
            }
        }

        /// <summary>
        /// Asynchronously waits to enter the SemaphoreSlim, executes the action and then releases the SemaphoreSlim.
        /// </summary>
        public async Task DoAsync(Action<TValue> action, CancellationToken? token = null)
        {
            try
            {
                await WaitAsync(token);
                action(Value);
            }
            //catch (OperationCanceledException) { }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                Release();
            }
        }

        /// <summary>
        /// Asynchronously waits to enter the SemaphoreSlim, executes the action and then releases the SemaphoreSlim.
        /// </summary>
        public async Task DoAsync(Func<TValue, Task> func, CancellationToken? token = null)
        {
            try
            {
                await WaitAsync(token);
                await func(Value);
            }
            //catch (OperationCanceledException) { }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                Release();
            }
        }
        
        /// <summary>
        /// Asynchronously waits to enter the SemaphoreSlim, executes the action and then releases the SemaphoreSlim.
        /// </summary>
        public async Task<TResult> DoAsync<TResult>(Func<TValue, TResult> func, CancellationToken? token = null)
        {
            try
            {
                await WaitAsync(token);
                return func(Value);
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                Release();
            }
        }

        /// <summary>
        /// Asynchronously waits to enter the SemaphoreSlim, executes the action and then releases the SemaphoreSlim.
        /// </summary>
        public async Task<TResult> DoAsync<TResult>(Func<TValue, Task<TResult>> func, CancellationToken? token = null)
        {
            try
            {
                await WaitAsync(token);
                return await func(Value);
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                Release();
            }
        }
    }
}
