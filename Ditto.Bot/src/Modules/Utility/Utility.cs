using Discord;
using Ditto.Attributes;
using Ditto.Data.Commands;
using Ditto.Data.Discord;
using System.Linq;
using System.Threading.Tasks;

namespace Ditto.Bot.Modules.Utility
{
    public class Utility : DiscordModule
    {
        //[DiscordCommand(CommandSourceLevel.Guild, CommandAccessLevel.All)]
        //public async Task Prune(IUser user, [Multiword] string pattern, int count = 100)
        //{
        //    if (count > 100)
        //        count = 100;
        //    else if (count <= 0)
        //        count = 1;
        //    var list = (await Context.Channel.GetMessagesAsync(count).ToList())
        //        .SelectMany(i => i)
        //        .Where(i => i.Content.Contains(pattern));
        //    foreach(var msg in list)
        //    {
        //        try
        //        {
        //            await msg.DeleteAsync().ConfigureAwait(false);
        //        }
        //        catch { }
        //    }
        //}
    }
}
