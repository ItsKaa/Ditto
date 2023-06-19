using Discord;
using Discord.Commands;
using Ditto.Attributes;
using Ditto.Data.Commands;
using Ditto.Data.Discord;
using System.Threading.Tasks;

namespace Ditto.Bot.Modules.Admin
{
    [Alias("admin")]
    public class AdminText : DiscordModule
    {
        [DiscordCommand(CommandSourceLevel.Guild, CommandAccessLevel.LocalAndParents)]
        public override Task _()
        {
            return Task.CompletedTask;
        }

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.All)]
        public async Task Debug(bool enable = true)
        {
            if (!Permissions.IsBotOwner(Context))
            {
                await Context.ApplyResultReaction(CommandResult.FailedUserPermission).ConfigureAwait(false);
            }
            else
            {
                Admin.DebugLogging(enable);
            }
        }

        [Alias("dc")]
        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.All)]
        public async Task Disconnect()
        {
            if (!Permissions.IsBotOwner(Context))
            {
                await Context.ApplyResultReaction(CommandResult.FailedUserPermission).ConfigureAwait(false);
            }
            else
            {
                await Admin.Disconnect();
            }
        }

        [DiscordCommand(CommandSourceLevel.Guild, CommandAccessLevel.All)]
        public async Task SetCache(ITextChannel textChannel)
        {
            if (!Permissions.IsBotOwner(Context))
            {
                await Context.ApplyResultReaction(CommandResult.FailedUserPermission).ConfigureAwait(false);
            }
            else if (!await Permissions.CanBotSendMessages(textChannel).ConfigureAwait(false))
            {
                await Context.ApplyResultReaction(CommandResult.FailedBotPermission).ConfigureAwait(false);
            }
            else
            {
                await Admin.SetGlobalCacheChannel(textChannel);
                await Context.ApplyResultReaction(CommandResult.Success).ConfigureAwait(false);
            }
        }
    }
}
