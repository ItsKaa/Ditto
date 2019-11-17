using Ditto.Attributes;
using Ditto.Data.Commands;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Ditto.Bot.Data.Reflection
{
    public class ModuleMethod
    {
        public List<string> Aliases { get; internal set; }
        public MethodInfo MethodInfo { get; internal set; }

        public int Priority { get; set; }
        public CommandSourceLevel Source { get; set; }
        public CommandAccessLevel Accessibility { get; set; }

        public string GetFullName()
        {
            //return string.Format("{0}.{1}({2})", MethodInfo.ReflectedType.FullName, MethodInfo.Name, string.Join(",", MethodInfo.GetParameters().Select(o => string.Format("{0} {1}", o.ParameterType, o.Name)).ToArray()));
            return string.Format("{0}.{1}({2})", MethodInfo.ReflectedType.FullName, MethodInfo.Name, string.Join(",", MethodInfo.GetParameters().Select(o => string.Format("{0}", o.ParameterType)).ToArray()));
            //return string.Format("{0}.{1}", MethodInfo.ReflectedType.FullName, MethodInfo.Name);
        }
    }
}
