using Discord.Interactions;

namespace Ditto.Data.Discord
{
    public abstract class DiscordSlashModule : ModuleSlashBaseClass
    {
        protected DiscordSlashModule(InteractionService interactionService)
            : base(interactionService)
        {
        }
    }
}
