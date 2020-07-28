using Ditto.Attributes;
using Ditto.Data.Commands;
using Ditto.Data.Discord;
using Google.Apis.Logging;
using System.Threading.Tasks;

namespace Ditto.Bot.Modules.Admin
{
    public class Admin : DiscordModule
    {
        [DiscordCommand(CommandSourceLevel.Guild, CommandAccessLevel.LocalAndParents)]
        public override Task _()
        {
            return Task.CompletedTask;
        }

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.Global)]
        public async Task Debug(bool enable = true)
        {
            if(!Permissions.IsBotOwner(Context))
            {
                await Context.ApplyResultReaction(CommandResult.FailedUserPermission).ConfigureAwait(false);
            }

            Log.Setup(Log.LogToConsole, Log.LogToFile, enable);
        }
    }
}
