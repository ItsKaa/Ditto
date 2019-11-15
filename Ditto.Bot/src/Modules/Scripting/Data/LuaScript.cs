using Discord;
using MoonSharp.Interpreter;

namespace Ditto.Bot.Modules.Scripting.Data
{
    public class LuaScript
    {
        public IGuild Guild { get; set; }
        
        public LuaDiscord Lua { get; set; }
        
        public Script Script { get; set; }
        
        public string FileName { get; set; }
        
        public string FilePath => FileName == null ? null : $"{Globals.AppDirectory}\\data\\lua\\{Guild?.Id}\\{FileName}";
    }
}
