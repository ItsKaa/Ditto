using System;

namespace Ditto.Bot.Services.Data
{
    public sealed class CacheItem<T, TResult>
        where T : class
        where TResult : class
    {
        public Func<T, TResult> Function { get; private set; }
        public TimeSpan Delay { get; private set; }
        public DateTime LastRefresh { get; private set; } = DateTime.MinValue;
        public TResult CachedValue { get; private set; } = null;
        
        public CacheItem(Func<T, TResult> function, TimeSpan delay)
        {
            Function = function;
            Delay = delay;
        }
        public TResult Refresh(T arg)
        {
            CachedValue = Function(arg);
            LastRefresh = DateTime.Now;
            return CachedValue;
        }
    }
}
