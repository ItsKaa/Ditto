using Ditto.Data;
using Ditto.Extensions;
using System;

namespace Ditto.Bot.Modules.BDO.Data
{
    public class BDOClock
    {
        public DateTime RealTimeUtc { get; private set; }
        public TimeSpan Time { get; private set; }
        public bool IsDay { get; private set; }
        public int SecondsUntilNight { get; private set; }
        public int SecondsUntilDay { get; private set; }

        public string UntilDayString { get { return new TimeSpan(0, 0, SecondsUntilDay).Without(TimeUnit.BeforeMinutes).Humanize(true, " "); } }
        public string UntilNightString { get { return new TimeSpan(0, 0, SecondsUntilNight).Without(TimeUnit.BeforeMinutes).Humanize(true, " "); } }
        public string UntilNightOrDayString { get { return IsDay ? $"Night in {UntilNightString}" : $"Day in {UntilDayString}"; } }

        public BDOClock()
        {
            RealTimeUtc = new DateTime();
            Time = new TimeSpan();
            IsDay = false;
            SecondsUntilNight = 0;
            SecondsUntilDay = 0;
        }
        
        public void Update()
        {
            var d = DateTime.UtcNow;
            var startHour = new DateTime(d.Year, d.Month, d.Day, 0, 0, 0);
            var secsIntoGameDay = ((d - startHour).TotalSeconds + 200 * 60 + 20 * 60) % (240 * 60);

            // Last part of the shifted day is night
            double gameHour = 0.0;
            if (secsIntoGameDay >= 12000)
            {
                var secsIntoGameNight = secsIntoGameDay - 12000;
                var pctOfNightDone = secsIntoGameNight / (40 * 60);
                gameHour = 9 * pctOfNightDone;
                gameHour = gameHour < 2 ? 22 + gameHour : gameHour - 2;
                var secsUntilNightEnd = 40 * 60 - secsIntoGameNight;

                IsDay = false;
                SecondsUntilDay = (int)secsUntilNightEnd;
                SecondsUntilNight = (int)(secsUntilNightEnd + 12000);
            }
            else
            {
                var secsIntoGameDaytime = secsIntoGameDay;
                var pctOfDayDone = secsIntoGameDay / (200 * 60);
                gameHour = 7 + (22 - 7) * pctOfDayDone;
                var secsUntilNightStart = 12000 - secsIntoGameDaytime;

                IsDay = true;
                SecondsUntilDay = (int)(secsUntilNightStart + 40 * 60);
                SecondsUntilNight = (int)secsUntilNightStart;
            }
            var hr_test = (int)Math.Truncate(gameHour);
            var min_test = (int)Math.Truncate(gameHour % 1 * 60);
            var sec_test = (int)Math.Truncate((gameHour % 1 * 60) % 1 * 60);
            Time = new TimeSpan((int)Math.Truncate(gameHour), (int)Math.Truncate(gameHour % 1 * 60), (int)Math.Truncate((gameHour % 1 * 60) % 1 * 60));
            RealTimeUtc = d;
        }
    }
}
