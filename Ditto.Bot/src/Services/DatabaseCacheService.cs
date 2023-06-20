using Discord;
using Ditto.Bot.Database;
using Ditto.Bot.Helpers;
using Ditto.Bot.Services;
using Ditto.Bot.Services.Data;
using Ditto.Extensions;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Ditto.Bot.Services
{
    public class DatabaseCacheService : IDittoService
    {
        protected ConcurrentDictionary<string, CacheItem<IUnitOfWork, object>> _cachedDatabaseItems = new ConcurrentDictionary<string, CacheItem<IUnitOfWork, object>>();

        public Task Initialised() => Task.CompletedTask;
        public Task Connected() => Task.CompletedTask;
        public Task Exit() => Task.CompletedTask;

        #region Properties
        public Color EmbedColour(IGuild guild) => GetOrAddCachededItem(CacheHelper.GetCacheName(guild),
            (uow) => uow.Configs.GetEmbedColour(guild).Value.ToColour()
            , Globals.Cache.DefaultCacheTime
        );

        public Color EmbedErrorColour(IGuild guild) => GetOrAddCachededItem(CacheHelper.GetCacheName(guild),
            (uow) => uow.Configs.GetEmbedErrorColour(guild).Value.ToColour()
            , Globals.Cache.DefaultCacheTime
        );

        public Color EmbedRssColour(IGuild guild) => GetOrAddCachededItem(CacheHelper.GetCacheName(guild),
            (uow) => uow.Configs.GetEmbedRssColour(guild).Value.ToColour()
            , Globals.Cache.DefaultCacheTime
        );

        public Color EmbedDiscordLinkColour(IGuild guild) => GetOrAddCachededItem(CacheHelper.GetCacheName(guild),
            (uow) => uow.Configs.GetEmbedDiscordLinkColour(guild).Value.ToColour()
            , Globals.Cache.DefaultCacheTime
        );

        public Color EmbedTwitchLinkColour(IGuild guild) => GetOrAddCachededItem(CacheHelper.GetCacheName(guild),
            (uow) => uow.Configs.GetEmbedTwitchLinkColour(guild).Value.ToColour()
            , Globals.Cache.DefaultCacheTime
        );

        public Color EmbedMusicPlayingColour(IGuild guild) => GetOrAddCachededItem(CacheHelper.GetCacheName(guild),
            (uow) => uow.Configs.GetEmbedMusicPlayingColour(guild).Value.ToColour()
            , Globals.Cache.DefaultCacheTime
        );

        public Color EmbedMusicPausedColour(IGuild guild) => GetOrAddCachededItem(CacheHelper.GetCacheName(guild),
            (uow) => uow.Configs.GetEmbedMusicPausedColour(guild).Value.ToColour()
            , Globals.Cache.DefaultCacheTime
        );

        public string Prefix(IGuild guild) => GetOrAddCachededItem(CacheHelper.GetCacheName(guild),
            (uow) => uow.Configs.GetPrefix(guild).Value
            , Globals.Cache.DefaultCacheTime
        );
        #endregion Properties

        /// <summary>
        /// Get a specific item from our cache. will return null if invalid.
        /// </summary>
        public virtual T Get<T>(string name) where T : class
        {
            if (_cachedDatabaseItems.TryGetValue(name, out CacheItem<IUnitOfWork, object> cacheItem))
            {
                return cacheItem?.CachedValue as T;
            }
            return null;
        }

        /// <summary>
        /// Clear everything stored in the cache.
        /// </summary>
        public virtual void Clear()
        {
            _cachedDatabaseItems.Clear();
        }

        /// <summary>
        /// Remove a specific item from the cache.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual bool Remove(string name)
        {
            if (_cachedDatabaseItems.TryRemove(name, out CacheItem<IUnitOfWork, object> cacheItem))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Add a cache item that interacts with the database, this item will only refresh if neccessary when this object is requested, much like Lazy.
        /// For the moment, items will not get deleted from memory unless Clear() is called.
        /// </summary>
        /// <typeparam name="T">type of the object</typeparam>
        /// <param name="name">name of the cached item, must be unique</param>
        /// <param name="func">function that interacts with the database, must return our object.</param>
        /// <param name="refreshDelay"></param>
        /// <param name="complete">see UnitOfWork.Complete()</param>
        /// <returns></returns>
        public virtual T GetOrAddCachededItem<T>(string name, Func<IUnitOfWork, T> func, TimeSpan refreshDelay, bool complete = false)
        {
            var cacheItem = _cachedDatabaseItems.GetOrAdd(name, new CacheItem<IUnitOfWork, object>(new Func<IUnitOfWork, object>((uow) =>
            {
                var result = func(uow);
                if (complete)
                    uow.Complete();
                return result;
            }), refreshDelay));
            if (DateTime.Now - cacheItem.LastRefresh > cacheItem.Delay)
            {
                return (T)cacheItem.Refresh(Ditto.Database.UnitOfWork);
            }
            return (T)cacheItem.CachedValue;
        }

        // Never tested and probably unneccessary.
        //public static async Task<T> GetOrAddCachededItemAsync<T>(string name, Func<UnitOfWork, Task<T>> func, TimeSpan refreshDelay, bool complete = true)
        //{
        //    var cacheItem = _cachedDatabaseItems.GetOrAdd(name, new CacheItem<UnitOfWork, object>(new Func<UnitOfWork, object>((uow) => func(uow)), complete, refreshDelay));
        //    if ((DateTime.Now - cacheItem.LastRefresh) > cacheItem.Delay)
        //    {
        //        var obj = cacheItem.Refresh(DatabaseHandler.UnitOfWork) as Task<T>;
        //        return await obj;
        //    }
        //    return await (cacheItem.CachedValue as Task<T>);
        //}
    }
}
