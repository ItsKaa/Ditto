using Discord.Commands;
using Ditto.Attributes;
using Ditto.Data.Commands;
using Ditto.Data.Discord;
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
                await Ditto.Client.DoAsync(async client =>
                {
                    await client.StopAsync().ConfigureAwait(false);
                }).ConfigureAwait(false);
            }
        }
    }
}
