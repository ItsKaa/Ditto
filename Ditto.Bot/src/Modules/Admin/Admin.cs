using Discord;
using Discord.Commands;
using Ditto.Attributes;
using Ditto.Data.Commands;
using Ditto.Data.Discord;
using System.Threading.Tasks;

namespace Ditto.Bot.Modules.Admin
{
    public class Admin : DiscordModule
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

        [DiscordCommand(CommandSourceLevel.Guild, CommandAccessLevel.LocalAndParents)]
        public override Task _()
        {
            return Task.CompletedTask;
        }

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.Global)]
        public async Task Debug(bool enable = true)
        {
            if (!Permissions.IsBotOwner(Context))
            {
                await Context.ApplyResultReaction(CommandResult.FailedUserPermission).ConfigureAwait(false);
            }
            else
            {
                Log.Setup(Log.LogToConsole, Log.LogToFile, enable);
            }
        }


        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.Global)]
        [Alias("dc")]
        public async Task Disconnect(bool enable = true)
        {
            if (!Permissions.IsBotOwner(Context))
            {
                await Context.ApplyResultReaction(CommandResult.FailedUserPermission).ConfigureAwait(false);
            }
            else
            {
                await Ditto.Client.StopAsync();
            }
        }

        [DiscordCommand(CommandSourceLevel.Guild, CommandAccessLevel.Local)]
        public async Task SetCache(ITextChannel textChannel)
        {
            if (!Permissions.IsBotOwner(Context))
            {
                await Context.ApplyResultReaction(CommandResult.FailedUserPermission).ConfigureAwait(false);
            }
            if (!await Permissions.CanBotSendMessages(textChannel).ConfigureAwait(false))
            {
                await Context.ApplyResultReaction(CommandResult.FailedBotPermission).ConfigureAwait(false);
            }
            else
            {
                await Ditto.Database.DoAsync(uow =>
                {
                    uow.Configs.SetGlobalCacheChannel(textChannel);
                }).ConfigureAwait(false);

                CacheChannel = textChannel;

                await Context.ApplyResultReaction(CommandResult.Success).ConfigureAwait(false);
            }
        }
    }
}
