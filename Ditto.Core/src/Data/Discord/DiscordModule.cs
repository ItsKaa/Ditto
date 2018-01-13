namespace Ditto.Data.Discord
{
    public abstract class DiscordModule : ModuleBaseClass
    {
        public DiscordModule()
        {
        }

        protected T Module<T>()
            where T: DiscordModule, new()
        {
            return new T() { Context = Context };
        }
    }
    
    public abstract class DiscordModule<T> : ModuleBaseClass
        //where T: DiscordModule, new()
        where T : ModuleBaseClass, new()
    {
        //protected bool Initialized { get; private set; }
        //protected bool Running { get; private set; }
        
        public DiscordModule()
        {
        }

        /// <summary>
        /// This is the default module caller, for if you only call your module name,
        /// e.g.: "help reminder"
        /// </summary>
        /// <returns></returns>
        //[DiscordCommand]
        //public virtual Task _() => Task.CompletedTask;
    }
}
