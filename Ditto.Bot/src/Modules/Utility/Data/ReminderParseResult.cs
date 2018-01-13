using System;

namespace Ditto.Bot.Modules.Utility.Data
{
    public struct ReminderParseResult
    {
        public string Text { get; set; }
        public string TimeString { get; set; }
        public TimeSpan? Time { get; set; }
        public string Error { get; set; }
        public bool Repeat { get; set; }

        public bool Success => (Error == null && Time.HasValue && TimeString != null);
    }
}
