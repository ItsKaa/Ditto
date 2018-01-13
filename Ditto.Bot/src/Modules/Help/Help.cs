using Discord.Commands;
using Ditto.Attributes;
using Ditto.Data.Commands;
using Ditto.Data.Discord;
using System.Threading.Tasks;

namespace Ditto.Bot.Modules.Help
{
    [Alias("description", "desc", "describe", "explain", "detail", "details", "info", "h")]
    public sealed class Help : DiscordModule
    {
        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.Local | CommandAccessLevel.Parents)]
        public Task _(string who = "")
        {
            return Task.CompletedTask;
        }
    }
}
