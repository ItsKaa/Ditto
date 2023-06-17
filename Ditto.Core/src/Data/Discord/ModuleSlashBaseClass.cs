using Discord.Interactions;
using System;

namespace Ditto.Data.Discord
{
    public abstract class ModuleSlashBaseClass : InteractionModuleBase, IDisposable
    {
        protected bool IsDisposing { get; private set; }
        private InteractionService InteractionService { get; }

        protected ModuleSlashBaseClass(InteractionService interactionService)
        {
            InteractionService = interactionService;
        }

        public void Dispose()
        {
            if (!IsDisposing)
            {
                IsDisposing = true;
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
        }

    }
}
