using Cauldron;
using Cauldron.Core.Collections;
using Ditto.Data.Commands;
using Ditto.Extensions;
using MoonSharp.Interpreter;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Ditto.Bot.Modules.Scripting.Data
{
    public static class LuaHandler
    {
        private static ConcurrentList<MethodInfo> _methods;
        private static ConcurrentList<PropertyInfo> _properties;
        private static ConcurrentList<FieldInfo> _fields;
        private static readonly Type _luaMethodsType = typeof(LuaDiscord);
        
        static LuaHandler()
        {   
            // Use reflection to find every method, field and property that is defined in the LuaDiscord class.
            _methods = new ConcurrentList<MethodInfo>(_luaMethodsType.GetMethods().Where(method =>
                method.IsPublic
                && !method.IsGenericMethod
                && !method.IsConstructor
                && !method.IsAbstract
                && !method.IsStatic
                && method.DeclaringType == _luaMethodsType
                && method.GetCustomAttribute<CompilerGeneratedAttribute>() == null
            ));
            _properties = new ConcurrentList<PropertyInfo>(_luaMethodsType.GetProperties(BindingFlags.Instance | BindingFlags.Public));
            _fields = new ConcurrentList<FieldInfo>(_luaMethodsType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static));
        }
        
        private static string CleanupCode(string luaCode)
        {
            if (luaCode == null)
                return null;

            var markdownSyntax = new string[]
            {
                "asciidoc",
                "autohotkey",
                "bash",
                "coffeescript",
                "cpp", "cplusplus",
                "cs", "csharp",
                "css",
                "diff",
                "fix",
                "glsl",
                "ini",
                "json",
                "md", "markdown",
                "ml",
                "prolog",
                "py",
                "tex",
                "xl",
                "xml",
            };

            foreach(var md in markdownSyntax)
            {
                luaCode = luaCode
                    .Replace( $"`{md}", "", StringComparison.CurrentCultureIgnoreCase)
                    .Replace($"` {md}", "", StringComparison.CurrentCultureIgnoreCase);
                ;
            }

            return luaCode.Replace("`", "");
        }

        /// <summary>
        /// Validate lua code from file
        /// </summary>
        public static bool Validate(LuaScript luaScript)
        {
            try
            {
                var value = luaScript.Script.LoadFile(luaScript.FilePath);
                return true;
            }
            catch(Exception) {}
            return false;
        }

        /// <summary>
        /// Validate lua code from file
        /// </summary>
        public static Task<bool> ValidateAsync(LuaScript luaScript)
        {
            return Task.Run(() => Validate(luaScript));
        }

        /// <summary>
        /// Validate lua code from memory
        /// </summary>
        public static bool Validate(string luaCode, ICommandContextEx context)
        {
            try
            {
                if (CreateLuaScript(out LuaScript luaScript, null, null, context))
                {
                    var value = luaScript.Script.LoadString(luaCode);
                    return true;
                }
            }
            catch (Exception) { }
            return false;
        }

        /// <summary>
        /// Validate lua code from memory
        /// </summary>
        public static Task<bool> ValidateAsync(string luaCode, ICommandContextEx context)
        {
            return Task.Run(() => Validate(luaCode, context));
        }

        public static DynValue Run(LuaScript luaScript)
        {
            DynValue value = null;
            if (luaScript != null)
            {
                SetScriptVariables(luaScript);

                if (File.Exists(luaScript.FilePath))
                {
                    value = luaScript.Script.DoFile(luaScript.FilePath);
                }
                else if (luaScript.Code != null)
                {
                    value = luaScript.Script.DoString(luaScript.Code);
                }

                if (value != null)
                {
                    ApplyScriptVariables(luaScript);
                }
            }
            return value;
        }

        public static Task<DynValue> RunAsync(LuaScript luaScript)
        {
            return Task.Run(() => Run(luaScript));
        }

        //public static DynValue Run(string luaCode, ICommandContextEx context)
        //{
        //    return Run(CreateLuaScript(luaCode, null, context));
        //}
        //public static Task<DynValue> RunAsync(string luaCode, ICommandContextEx context)
        //{
        //    return Task.Run(() => Run(luaCode, context));
        //}
        
        public static bool CreateLuaScript(out LuaScript luaScript, string luaCode, string fileName, ICommandContextEx context)
        {
            luaScript = new LuaScript
            {
                Guild = context?.Guild,
                Code = null,
                //Code = CleanupCode(luaCode),
                //Script = new Script(CoreModules.Preset_HardSandbox),
                Script = new Script(),
                //Channel = context?.Channel as ITextChannel,
                FileName = fileName,
                Lua = new LuaDiscord()
                {
                    User = context?.User,
                    Channel = context?.Channel,
                    Role = null
                }
            };
            

            SetScriptVariables(luaScript);
            luaCode = CleanupCode(luaCode);

            if (fileName != null)
            {
                //luaScript.Script.Options.ScriptLoader = new FileSystemScriptLoader();

                // Add data to our code
                luaCode =
$@"function Initialise()
    ChannelId = '{luaScript?.Lua?.Channel?.Id ?? 0}';
end

{luaCode}";

                // Validate our code
                if (!Validate(luaCode, context))
                {
                    return false;
                }

                // Create and write to file
                Directory.CreateDirectory(Path.GetDirectoryName(luaScript.FilePath));
                if(File.Exists(luaScript.FilePath))
                {
                    File.Delete(luaScript.FilePath);
                }

                using (var sw = new StreamWriter(File.Open(luaScript.FilePath, FileMode.OpenOrCreate), Encoding.UTF8))
                {
                    sw.Write(luaCode);
                }
            }
            else
            {
                luaScript.Code = luaCode;
            }
            return true;
        }

        private static LuaScript SetScriptVariables(LuaScript luaScript)
        {
            // Define every method, field and property as a global entry.
            _fields.Foreach(field => luaScript.Script.Globals[field.Name] = field.GetValue(luaScript.Lua));
            _properties.Foreach(prop => luaScript.Script.Globals[prop.Name] = prop.GetValue(luaScript.Lua));
            _methods.Foreach(method => luaScript.Script.Globals[method.Name] = method.CreateDelegate(luaScript.Lua));
            return luaScript;
        }

        public static LuaScript ApplyScriptVariables(LuaScript luaScript, bool ignorePrivateSetters = false)
        {
            _properties.ToList().ForEach(prop =>
            {
                if (prop.CanWrite && (ignorePrivateSetters || prop.SetMethod.IsPublic))
                {
                    try
                    {
                        prop.SetValue(luaScript.Lua, luaScript.Script.Globals[prop.Name]);
                    }
                    catch
                    {
                        prop.SetValue(luaScript.Lua, Convert.ChangeType(luaScript.Script.Globals[prop.Name], prop.PropertyType));
                    }
                }
            });
            _fields.Where(f => !f.IsLiteral).Foreach(field => field.SetValue(luaScript.Lua, luaScript.Script.Globals[field.Name]));
            return luaScript;
        }
    }
}
