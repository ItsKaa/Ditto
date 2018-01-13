using System;
using System.Collections.Generic;
using System.Reflection;

namespace Ditto.Extensions
{
    public static class TypeExtensions
    {
        public static IEnumerable<Type> GetParentTypes(this Type type)
        {
            // is there any base type?
            if ((type == null) || (type.GetTypeInfo().BaseType == null))
            {
                yield break;
            }

            // return all implemented or inherited interfaces
            foreach (var i in type.GetInterfaces())
            {
                yield return i;
            }

            // return all inherited types
            var currentBaseType = type.GetTypeInfo().BaseType;
            while (currentBaseType != null)
            {
                yield return currentBaseType;
                currentBaseType = currentBaseType.GetTypeInfo().BaseType;
            }
        }

        public static Type GetParentType(this Type type)
        {
            return type?.GetTypeInfo().UnderlyingSystemType ?? type?.DeclaringType ?? type;
        }

        public static Type GetParentType(this Object obj)
        {
            return obj?.GetType()?.GetParentType();
        }
    }
}
