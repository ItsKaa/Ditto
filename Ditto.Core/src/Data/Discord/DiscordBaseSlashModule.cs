using Discord.Interactions;

namespace Ditto.Data.Discord
{
    public abstract class DiscordBaseSlashModule : ModuleSlashBaseClass
    {
        protected DiscordBaseSlashModule(InteractionService interactionService)
            : base(interactionService)
        {
        }
    }
}
