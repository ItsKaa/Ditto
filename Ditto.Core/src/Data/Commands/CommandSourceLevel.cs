using System;

namespace Ditto.Data.Commands
{
    [Flags]
    public enum CommandSourceLevel
    {
        Guild   = 1 << 0,
        DM      = 1 << 1,
        Group   = 1 << 2,

        All = Guild | DM | Group
    }
}
