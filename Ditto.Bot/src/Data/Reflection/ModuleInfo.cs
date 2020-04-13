using Ditto.Attributes;
using Ditto.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Ditto.Bot.Data.Reflection
{
    public class ModuleInfo
    {
        public List<string> Aliases { get; set; }
        public Type Type { get; set; }
        public Type ParentType { get; set; }
        public int Priority { get; set; }

        public List<ModuleInfo> ParentModules { get; set; }
        public List<ModuleInfo> SubModules { get; set; }
        public List<ModuleMethod> Methods { get; set; }

        public ModuleInfo()
        {
            Aliases = new List<string>();
            Priority = -1;
            ParentModules = new List<ModuleInfo>();
            SubModules = new List<ModuleInfo>();
            Methods = new List<ModuleMethod>();
        }

        public string GetHelpNameString(string separator = ".")
        {
            var value = string.Empty;
            foreach (var parentModule in ParentModules)
            {
                var parentValue = parentModule.GetHelpNameString(separator);
                if (!string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(parentValue))
                {
                    value += separator;
                }
                if (!string.IsNullOrEmpty(parentValue))
                {
                    value += parentValue;
                }
            }

            if (!string.IsNullOrEmpty(value))
            {
                value += separator;
            }
            value += ToString();
            return value;
        }

        public override string ToString()
        {
            var helpAttribute = Type.GetCustomAttributes<HelpAttribute>().FirstOrDefault();
            return (helpAttribute?.Name ?? Aliases?.FirstOrDefault()?.ToTitleCase() ?? Type.Name.ToTitleCase());
        }
    }
}
