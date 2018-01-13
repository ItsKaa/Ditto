using System;

namespace Ditto.Bot.Database.Data
{
    [Flags]
    public enum BdoServerStatus
    {
        Unknown = 0x01,
        Offline = 0x02,
        Online = 0x04,
        Maintenance = 0x08,
    }
}
