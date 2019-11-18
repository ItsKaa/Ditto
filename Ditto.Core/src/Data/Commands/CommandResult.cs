using System;

namespace Ditto.Data.Commands
{
    [Flags]
    public enum CommandResult
    {
        None                 = 1 << 0,

        Success              = 1 << 1,
        SuccessAlt1          = 1 << 2,
        SuccessAlt2          = 1 << 3,

        Failed               = 1 << 4,
        InvalidParameters    = 1 << 5,
        FailedBotPermission  = 1 << 6,
        FailedUserPermission = 1 << 7,
    }
}
