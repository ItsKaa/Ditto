using Ditto.Bot.Data.Reflection;
using System.Collections.Generic;

namespace Ditto.Bot.Services.Commands.Data
{
    public class ParseResult
    {
        public string InputMessage { get; set; }
        public string ErrorMessage { get; set; }

        public List<object[]> Parameters { get; set; }
        public ModuleInfo Module { get; set; }
        public ModuleMethod Method { get; set; }
        public int Score { get; set; }
        public int Priority { get; set; }
    }
}
