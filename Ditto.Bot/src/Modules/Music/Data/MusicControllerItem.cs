using Ditto.Data.Discord;
using System;
using System.Threading.Tasks;

namespace Ditto.Bot.Modules.Music.Data
{
    internal class MusicControllerItem
    {
        public Emotes Emote { get; set; }
        public bool Display { get; set; }
        public bool IsEnabled { get; set; }
        public Func<Task> Add { get; set; }
        public Func<Task> Remove { get; set; }
        public Func<Task> AddOrRemove { get; set; }

        public MusicControllerItem(Emotes emote, Func<Task> add, Func<Task> remove, Func<Task> addOrRemove = null, bool display = false)
        {
            Emote = emote;
            Display = display;
            IsEnabled = false;

            if (addOrRemove != null)
            {
                AddOrRemove = () =>
                {
                    IsEnabled = !IsEnabled;
                    return addOrRemove();
                };
            }
            else
            {
                AddOrRemove = null;
                Add = async () =>
                {
                    IsEnabled = !IsEnabled;
                    if (add != null)
                    {
                        await add();
                    }
                };
                Remove = async () =>
                {
                    IsEnabled = !IsEnabled;
                    if(remove != null)
                    {
                        await remove();
                    }
                };
            }
        }
        public MusicControllerItem(Emotes emote, Func<Task> addOrRemove, bool display = false)
            : this(emote, null, null, addOrRemove, display)
        {
        }
    }
}
