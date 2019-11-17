using System;
using System.Collections.Generic;

namespace Ditto.Bot.Data.Reflection
{
    public class ModuleInfo
    {
        public List<string> Aliases { get; set; }
        public Type Type { get; set; }
        public Type ParentType { get; set; }
        public int Priority { get; set; }

        public List<ModuleInfo> SubModules { get; set; }
        public List<ModuleMethod> Methods { get; set; }
    }
}
