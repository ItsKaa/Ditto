using Discord.Commands;
using Ditto.Attributes;
using Ditto.Data.Commands;
using Ditto.Data.Discord;
using System.Threading.Tasks;

namespace Ditto.Bot.Modules.Admin
{
    [Alias("module")]
    public class Modules : DiscordModule<Admin>
    {
        [DiscordCommand(CommandSourceLevel.Guild, CommandAccessLevel.LocalAndParents)]
        public override Task _()
        {
            return List();
        }

        [DiscordCommand(CommandSourceLevel.Guild, CommandAccessLevel.Local)]
        [Alias("l", "show", "display", "view")]
        public Task List()
        {
            return Task.CompletedTask;
        }

        [DiscordCommand(CommandSourceLevel.Guild, CommandAccessLevel.Local)]
        [Alias("s", "status")]
        public Task Status()
        {
            return Task.CompletedTask;
        }

        [DiscordCommand(CommandSourceLevel.Guild, CommandAccessLevel.Local)]
        [Alias("e", "add", "start")]
        public Task Enable()
        {
            return Task.CompletedTask;
        }

        [DiscordCommand(CommandSourceLevel.Guild, CommandAccessLevel.Local)]
        [Alias("d", "remove", "rem", "stop")]
        public Task Disable()
        {
            return Task.CompletedTask;
        }

        [DiscordCommand(CommandSourceLevel.Guild, CommandAccessLevel.Local)]
        [Alias("r", "refresh", "update")]
        public Task Reload()
        {
            return Task.CompletedTask;
        }
    }
}
