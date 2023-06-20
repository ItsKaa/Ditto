using Ditto.Extensions;
using System;

namespace Ditto.Data.Discord
{
    public abstract class DiscordBaseTextModule : ModuleBaseClass
    {
        protected T Module<T>(IServiceProvider serviceProvider)
            where T: DiscordBaseTextModule
        {
            var instance = serviceProvider == null
                ? typeof(T).CreateInstance() as DiscordBaseTextModule
                : typeof(T).CreateInstanceWithServices(serviceProvider) as DiscordBaseTextModule;
            instance.Context = Context;
            return (T)instance;
        }
    }
    
    public abstract class DiscordBaseTextModule<T> : ModuleBaseClass
        where T : ModuleBaseClass
    {
    }
}
