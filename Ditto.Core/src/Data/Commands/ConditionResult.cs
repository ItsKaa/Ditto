using System;

namespace Ditto.Data.Commands
{
    public class ConditionResult
    {
        public bool DisplayError { get; private set; }
        public String ErrorMessage { get; private set; } = String.Empty;

        public bool IsOk => (ErrorMessage == null || ErrorMessage.Length == 0);
        public bool HasError => ErrorMessage?.Length > 0;

        private ConditionResult() { }
        public static ConditionResult FromError(String error, bool display = false)
        {
            return new ConditionResult()
            {
                ErrorMessage = error,
                DisplayError = display
            };
        }
        public static ConditionResult FromSuccess()
        {
            return new ConditionResult();
        }
    }
}
