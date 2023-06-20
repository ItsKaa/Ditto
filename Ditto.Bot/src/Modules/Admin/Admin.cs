using Discord;
using Ditto.Bot.Services;
using Ditto.Data.Discord;
using System.Threading.Tasks;

namespace Ditto.Bot.Modules.Admin
{
    public class Admin : DiscordTextModule
    {
        public static ITextChannel CacheChannel { get; private set; }

        static Admin()
        {
            Ditto.Connected += async () =>
            {
                var cacheChannelConfig = await Ditto.Database.ReadAsync(uow => uow.Configs.GetGlobalCacheChannel()).ConfigureAwait(false);
                if (ulong.TryParse(cacheChannelConfig?.Value, out ulong channelId))
                {
                    CacheChannel = await Ditto.Client.GetChannelAsync(channelId, new RequestOptions() { RetryMode = RetryMode.AlwaysRetry }) as ITextChannel;
                }
            };
        }

        public Admin(DatabaseCacheService cache, DatabaseService database) : base(cache, database)
        {
        }

        public static void DebugLogging(bool enable = true)
        {
            Log.Setup(Log.LogToConsole, Log.LogToFile, enable);
        }

        public static Task Disconnect()
        {
            return Ditto.Client.StopAsync();
        }

        public static async Task SetGlobalCacheChannel(ITextChannel textChannel)
        {
            await Ditto.Database.DoAsync(uow => uow.Configs.SetGlobalCacheChannel(textChannel));
            CacheChannel = textChannel;
        }
    }
}
