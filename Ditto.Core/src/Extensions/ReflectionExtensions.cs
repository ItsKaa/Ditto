using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Ditto.Extensions
{
    // Attribute
    public static partial class ReflectionExtensions
    {
        public static bool Contains(this IEnumerable<Attribute> attributes, Type type)
        {
            return attributes.FirstOrDefault(x => x.GetParentType() == type) != null;
        }
    }

    // ParameterInfo
    public static partial class ReflectionExtensions
    {
        /// <summary>
        /// Retrieve the specified default value or get the default value from the parameter type.
        /// </summary>
        public static object GetDefaultValue(this ParameterInfo parameterInfo)
        {
            return parameterInfo.HasDefaultValue
                ? parameterInfo.DefaultValue
                : parameterInfo.ParameterType.GetDefaultValue();
        }
    }
}
