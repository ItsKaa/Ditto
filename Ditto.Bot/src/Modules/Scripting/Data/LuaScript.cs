using Discord;
using MoonSharp.Interpreter;
using System;

namespace Ditto.Bot.Modules.Scripting.Data
{
    public class LuaScript
    {
        private static object _lock = new object();
        private static int _idCounter = 0;

        public LuaScript()
        {
            lock (_lock)
            {
                Id = ++_idCounter;
            }
        }

        public int Id { get; private set; }

        public IGuild Guild { get; set; }
        
        public LuaDiscord Lua { get; set; }
        
        public Script Script { get; set; }
        
        public string FileName { get; set; }
        
        public string FilePath => FileName == null ? null : $"{Globals.AppDirectory}\\data\\lua\\{Guild?.Id}\\{FileName}";

        public string Code { get; set; }

        public DateTime LastTickDate { get; set; } = DateTime.MinValue;
    }
}
