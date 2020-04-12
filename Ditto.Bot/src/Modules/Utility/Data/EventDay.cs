using Ditto.Data;
using System;

namespace Ditto.Bot.Modules.Utility.Data
{
    [Flags]
    public enum EventDay
    {
        Monday = Day.Monday,
        Tuesday = Day.Tuesday,
        Wednesday = Day.Wednesday,
        Thursday = Day.Thursday,
        Friday = Day.Friday,
        Saturday = Day.Saturday,
        Sunday = Day.Sunday,
        All = Monday | Tuesday | Wednesday | Thursday | Friday | Saturday | Sunday,
        Daily = All,
    }
}
