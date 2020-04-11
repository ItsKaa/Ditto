using System;

namespace Ditto.Bot.Modules.Scripting.Data
{
    [Flags]
    public enum LuaScriptMethods
    {
        Initialise      = 1 << 0,
        Main            = 1 << 1,
        UserJoined      = 1 << 2,
        RoleChanged     = 1 << 3,
        MessageReceived = 1 << 4,
        Tick            = 1 << 5,
    }
}
