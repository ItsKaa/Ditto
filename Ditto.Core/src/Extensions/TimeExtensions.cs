using Ditto.Data;
using System;
using System.Linq;

namespace Ditto.Extensions
{
    public static class TimeExtensions
    {
        public static string Humanize(this TimeSpan time, bool shortNames = true, string seperator = ", ")
        {
            var day = !shortNames ? "day" : "d";
            var hour = !shortNames ? "hour" : "h";
            var minute = !shortNames ? "minute" : "m";
            var second = !shortNames ? "second" : "s";
            var millisecond = !shortNames ? "millisecond" : "ms";

            var parts = string.Format($"{{0:D}} {day}:{{1:D}} {hour}:{{2:D}} {minute}:{{3:D}} {second}:{{4:D}} {millisecond}",
                time.Days,
                time.Hours,
                time.Minutes,
                time.Seconds,
                time.Milliseconds
            ).Split(':')
            .Where(e => !e.StartsWith("0")) // skip zero-valued components
            .Select(e =>
            {
                if(int.TryParse(new string(e.Before(' ').ToArray()), out int value))
                {
                    if(shortNames)
                    {
                        var test = value + new string(e.After(' ').ToArray());
                        return test;
                    }
                    else if (value > 1)
                    {
                        return e + "s";
                    }
                }
                return e;
            });
            var result = string.Join(seperator, parts);
            if(!shortNames)
            {
                var index = result.LastIndexOf(seperator);
                if(index > 0)
                {
                    result = result.Remove(index, seperator.Length).Insert(index, " and ");
                }
            }
            return result;
        }
        
        public static TimeSpan Get(this TimeSpan time, TimeUnit unit)
        {
            var newUnit = new TimeUnit();
            if (!unit.HasFlag(TimeUnit.Milliseconds))
            {
                newUnit = newUnit.Add(TimeUnit.Milliseconds);
            }
            if (!unit.HasFlag(TimeUnit.Seconds))
            {
                newUnit = newUnit.Add(TimeUnit.Seconds);
            }
            if (!unit.HasFlag(TimeUnit.Minutes))
            {
                newUnit = newUnit.Add(TimeUnit.Minutes);
            }
            if (!unit.HasFlag(TimeUnit.Hours))
            {
                newUnit = newUnit.Add(TimeUnit.Hours);
            }
            if (!unit.HasFlag(TimeUnit.Days))
            {
                newUnit = newUnit.Add(TimeUnit.Days);
            }
            return Without(time, newUnit);
        }
        public static TimeSpan Without(this TimeSpan time, TimeUnit unit)
        {
            var timeCopy = new TimeSpan(time.Ticks);

            if(unit.Has(TimeUnit.Milliseconds))
            {
                timeCopy = timeCopy.Subtract(new TimeSpan(0, 0, 0, 0, time.Milliseconds));
            }
            if (unit.Has(TimeUnit.Seconds))
            {
                timeCopy = timeCopy.Subtract(new TimeSpan(0, 0, 0, time.Seconds));
            }
            if (unit.Has(TimeUnit.Minutes))
            {
                timeCopy = timeCopy.Subtract(new TimeSpan(0, time.Minutes, 0));
            }
            if (unit.Has(TimeUnit.Hours))
            {
                timeCopy = timeCopy.Subtract(new TimeSpan(time.Hours, 0, 0));
            }
            if (unit.Has(TimeUnit.Days))
            {
                timeCopy = timeCopy.Subtract(new TimeSpan(time.Days, 0, 0, 0));
            }
            return timeCopy;
        }

    }
}
