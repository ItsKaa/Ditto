using Ditto.Bot.Services;
using Ditto.Data.Discord;

namespace Ditto.Bot.Modules
{
    public class DiscordTextModule : DiscordBaseTextModule
    {
        public DatabaseCacheService Cache { get; }
        public DatabaseService Database { get; }

        public DiscordTextModule(
            DatabaseCacheService cache,
            DatabaseService database
        )
        {
            Cache = cache;
            Database = database;
        }
    }


    public class DiscordTextModule<T> : DiscordBaseTextModule<T>
        where T : ModuleBaseClass
    {
        public DatabaseCacheService Cache { get; }
        public DatabaseService Database { get; }

        public DiscordTextModule(
            DatabaseCacheService cache,
            DatabaseService database
        )
        {
            Cache = cache;
            Database = database;
        }
    }

}
