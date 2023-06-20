using Discord.Interactions;
using Ditto.Bot.Services;
using Ditto.Data.Discord;

namespace Ditto.Bot.Modules
{
    internal class DiscordSlashModule : DiscordBaseSlashModule
    {
        public DatabaseCacheService Cache { get; }
        public DatabaseService Database { get; }

        public DiscordSlashModule(
            InteractionService interactionService,
            DatabaseCacheService cache,
            DatabaseService database
        )
            : base(interactionService)
        {
            Cache = cache;
            Database = database;
        }
    }
}
