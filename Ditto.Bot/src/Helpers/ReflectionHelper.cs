using Discord.Commands;
using Ditto.Attributes;
using Ditto.Bot.Data.Reflection;
using Ditto.Data.Commands;
using Ditto.Data.Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ModuleInfo = Ditto.Bot.Data.Reflection.ModuleInfo;

namespace Ditto.Bot.Helpers
{
    public class ReflectionHelper
    {
        public static IEnumerable<ModuleMethod> EnumerateMethods(IEnumerable<ModuleInfo> modules, ModuleInfo moduleInfo = null)
        {
            var list = new List<ModuleMethod>();
            if (moduleInfo == null)
            {
                foreach (var module in modules)
                {
                    var methods = EnumerateMethods(modules, module);
                    foreach (var m in methods)
                        list.Add(m);
                }
            }
            else
            {
                foreach (var m in moduleInfo.Methods)
                {
                    list.Add(m);
                }

                if (moduleInfo.SubModules != null)
                {
                    foreach (var m in moduleInfo.SubModules)
                    {
                        foreach (var sub in EnumerateMethods(modules, m))
                            list.Add(sub);
                    }
                }
            }
            return list;
        }

        public static IEnumerable<ModuleInfo> GetModules(Type baseType = null, ModuleInfo parentModule = null)
        {
            if (baseType == null)
            {
                // get top-level modules
                var searchType = typeof(DiscordModule);

                foreach(var moduleInfo in from t in Assembly.GetExecutingAssembly().GetTypes()
                        where t.BaseType == searchType
                            && t.GetConstructor(Type.EmptyTypes) != null
                        select new ModuleInfo()
                        {
                            Type = t,
                            //SubModules = GetModules(t).ToList(),
                            Methods = GetMethods(t),
                            Aliases = GetModuleAliases(t).ToList(),
                            Priority = t.GetCustomAttribute<PriorityAttribute>()?.Priority ?? 0
                        }
                )
                {
                    moduleInfo.SubModules = GetModules(moduleInfo.Type, moduleInfo).ToList();
                    if (parentModule != null)
                    {
                        moduleInfo.ParentModules.Add(parentModule);
                    }
                    yield return moduleInfo;
                }
            }
            else
            {
                // Get submodules
                foreach (var moduleInfo in from t in Assembly.GetExecutingAssembly().GetTypes()
                        where t.UnderlyingSystemType != baseType
                        && t.BaseType != null
                        && t.GetConstructor(Type.EmptyTypes) != null // we need a public constructor
                        && t.BaseType.IsGenericType
                        && t.BaseType.GetGenericArguments()?.Where(x => x.Equals(baseType)).Count() >= 1
                        select new ModuleInfo()
                        {
                            Type = t,
                            ParentType = baseType,
                            Methods = GetMethods(t),
                            //SubModules = GetModules(t),
                            Aliases = GetModuleAliases(t).ToList(),
                            Priority = t.GetCustomAttribute<PriorityAttribute>()?.Priority ?? 0
                        }
                )
                {
                    moduleInfo.SubModules = GetModules(moduleInfo.Type, moduleInfo).ToList();
                    if (parentModule != null)
                    {
                        moduleInfo.ParentModules.Add(parentModule);
                    }
                    yield return moduleInfo;
                }
            }
        }

        public static List<ModuleMethod> GetMethods(Type type)
        {
            return (from method in type.GetMethods()
                    where method.IsPublic
                    && !method.IsGenericMethod
                    && !method.IsConstructor
                    && !method.IsAbstract
                    && !method.IsStatic
                    //&& method.ReturnType == typeof(Task) || method.ReturnType == typeof(void) // We don't really care, it can be handy to return a bool/object
                    && method.IsDefined(typeof(DiscordCommandAttribute))
                    select new ModuleMethod()
                    {
                        MethodInfo = method,
                        Accessibility = (method.GetCustomAttribute(typeof(DiscordCommandAttribute)) as DiscordCommandAttribute)?.AccessLevel ?? CommandAccessLevel.Local,
                        Source = (method.GetCustomAttribute(typeof(DiscordCommandAttribute)) as DiscordCommandAttribute)?.SourceLevel ?? CommandSourceLevel.All,
                        Priority = (method.GetCustomAttribute(typeof(PriorityAttribute)) as PriorityAttribute)?.Priority ?? int.MaxValue,
                        Aliases = GetMethodAliases(method).ToList()
                    }
            ).ToList();
        }

        private static IEnumerable<string> GetModuleAliases(Type type)
        {
            if (type != null)
            {
                var list = new List<string>() { type.Name.ToLowerInvariant() };
                if (type.GetCustomAttribute(typeof(AliasAttribute)) is AliasAttribute aliasAttribute)
                {
                    list.AddRange(aliasAttribute.Aliases.Select(a => a.ToLowerInvariant()));
                }
                return list;
            }
            return Enumerable.Empty<string>();
        }

        private static IEnumerable<string> GetMethodAliases(MethodInfo methodInfo)
        {
            if (methodInfo != null)
            {
                var list = new List<string>() { methodInfo.Name.ToLowerInvariant() };
                if (methodInfo.GetCustomAttribute(typeof(AliasAttribute)) is AliasAttribute aliasAttribute)
                {
                    list.AddRange(aliasAttribute.Aliases.Select(a => a.ToLowerInvariant()));
                }
                return list;
            }
            return Enumerable.Empty<string>();
        }
    }
}
