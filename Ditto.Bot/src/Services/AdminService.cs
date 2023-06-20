using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace Ditto.Bot.Services
{
    public class AdminService : IDittoService
    {
        private DiscordSocketClient DiscordClient { get; }
        private DatabaseService DatabaseService { get; }
        public static ITextChannel CacheChannel { get; private set; }

        public AdminService(DiscordSocketClient discordClient, DatabaseService databaseService)
        {
            DiscordClient = discordClient;
            DatabaseService = databaseService;
        }

        public Task Initialised() => Task.CompletedTask;

        public async Task Connected()
        {
            var cacheChannelConfig = await DatabaseService.ReadAsync(uow => uow.Configs.GetGlobalCacheChannel()).ConfigureAwait(false);
            if (ulong.TryParse(cacheChannelConfig?.Value, out ulong channelId))
            {
                CacheChannel = await DiscordClient.GetChannelAsync(channelId, new RequestOptions() { RetryMode = RetryMode.AlwaysRetry }) as ITextChannel;
            }
        }

        public Task Exit() => Task.CompletedTask;

        public void DebugLogging(bool enable = true)
        {
            Log.Setup(Log.LogToConsole, Log.LogToFile, enable);
        }

        public Task Disconnect()
        {
            return DiscordClient.StopAsync();
        }

        public async Task SetGlobalCacheChannel(ITextChannel textChannel)
        {
            await DatabaseService.DoAsync(uow => uow.Configs.SetGlobalCacheChannel(textChannel));
            CacheChannel = textChannel;
        }
    }
}
