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
    }
}
