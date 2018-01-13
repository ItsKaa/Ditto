using System;

namespace Ditto.Data
{
    [Flags]
    public enum TimeUnit
    {
        Milliseconds = 1 << 0,
        Seconds = 1 << 1,
        Minutes = 1 << 2,
        Hours = 1 << 3,
        Days = 1 << 4,
        
        FromHours = Hours | Days,
        FromMinutes = Minutes | FromHours,
        FromSeconds = Seconds | FromMinutes,
        
        BeforeDays = Hours | BeforeHours,
        BeforeHours = Minutes | BeforeMinutes,
        BeforeMinutes = Seconds | BeforeSeconds,
        BeforeSeconds = Milliseconds,

        All = Milliseconds | FromSeconds,
    }
}
