using Ditto.Data;
using System;

namespace Ditto.Helpers
{
    public static class DateHelper
    {
        public static bool HasDayOfWeek(Day days, DayOfWeek dayOfWeek)
        {
            return dayOfWeek switch
            {
                DayOfWeek.Monday => days.HasFlag(Day.Monday),
                DayOfWeek.Tuesday => days.HasFlag(Day.Tuesday),
                DayOfWeek.Wednesday => days.HasFlag(Day.Wednesday),
                DayOfWeek.Thursday => days.HasFlag(Day.Thursday),
                DayOfWeek.Friday => days.HasFlag(Day.Friday),
                DayOfWeek.Saturday => days.HasFlag(Day.Saturday),
                DayOfWeek.Sunday => days.HasFlag(Day.Sunday),
                _ => false,
            };
        }
    }
}
