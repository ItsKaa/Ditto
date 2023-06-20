using Ditto.Bot.Services;

namespace Ditto.Bot.Modules.Utility
{
    public class Utility : DiscordTextModule
    {
        public Utility(DatabaseCacheService cache, DatabaseService database) : base(cache, database)
        {
        }
    }
}
