using System;

namespace Ditto.Bot.Modules.Scripting.Data
{
    [Flags]
    public enum LuaScriptMethods
    {
        Initialise      = 1 << 0,
        UserJoined      = 1 << 1,
        RoleChanged     = 1 << 2,
        MessageReceived = 1 << 3,
    }
}
