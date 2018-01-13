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
        public static bool IsOptional(this ParameterInfo @this)
        {
            // HasDefaultValue: declared a default within the method declaration
            // IsOptional could be an attribute.

            return @this.HasDefaultValue;
        }
    }
}
