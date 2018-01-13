using Discord.Commands;
using Ditto.Data.Commands;
using System;
using System.Threading.Tasks;

namespace Ditto.Data.Discord
{
    public abstract class ModuleBaseClass : ModuleBase, IDisposable
    {
        public new ICommandContextEx Context { get; set; }
        protected bool IsDisposing { get; private set; } = false;

        public void Dispose()
        {
            if(!IsDisposing)
            {
                IsDisposing = true;
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }
        protected virtual void Dispose(bool disposing)
        {
        }


        /// <summary>
        /// This is the default module caller, for if you only call your module name,
        /// e.g.: "help reminder"
        /// </summary>
        /// <returns></returns>
        public virtual Task _() => Task.CompletedTask;
    }
}
