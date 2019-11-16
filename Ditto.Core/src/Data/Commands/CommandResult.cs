using System;

namespace Ditto.Data.Commands
{
    [Flags]
    public enum CommandResult
    {
        None                 = 1 << 0,
        Success              = 1 << 1,
        Failed               = 1 << 2,
        InvalidParameters    = 1 << 3,
        FailedBotPermission  = 1 << 4,
        FailedUserPermission = 1 << 5,
    }
}
