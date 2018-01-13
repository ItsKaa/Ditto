using Discord;
using Discord.Commands;
using Ditto.Attributes;
using Ditto.Bot.Data;
using Ditto.Data.Commands;
using Ditto.Data.Discord;
using Ditto.Extensions;
using Ditto.Helpers;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Ditto.Bot.Modules.Utility
{
    [Alias("search")]
    public class SearchUtility : DiscordModule<Utility>
    {
        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.All)]
        public async Task Gif([Multiword] string query = null)
        {
            var result = await Ditto.Giphy.RandomAsync(query);

            // Slower but prettier, however limited by the discord size:
            //var httpWebRequest = (HttpWebRequest)WebRequest.Create(result.DirectUrl);
            //var httpWebReponse = (HttpWebResponse)httpWebRequest.GetResponse();
            //Stream stream = httpWebReponse.GetResponseStream();
            //await Context.Channel.SendFileAsync(stream, (Path.GetFileName(result.Url)?.Replace($"-{result.Id}", "") ?? "kaa") + ".gif").ConfigureAwait(false);

            // Faster, bit small but still pretty
            //await Context.Channel.EmbedAsync(new EmbedBuilder()
            //        .WithImageUrl(result.DirectUrl)
            //        .WithDescription(Context.Mention)
            //).ConfigureAwait(false);

            // Fastest, bigger but ugly:
            await Context.Channel.SendMessageAsync(
                $"{Context.User.Mention} {result.ShortDirectUrl ?? result.DirectUrl}"
            ).ConfigureAwait(false);
        }
        
        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.All)]
        public Task Define([Multiword] string query)
        {
            var result = UrbanDictionary.Define(query);
            if(result != null)
            {
                var definition = result.Definitions.FirstOrDefault();
                Context.EmbedAsync(new EmbedBuilder()
                    .WithTitle($":bookmark_tabs: {definition.Word.ToTitleCase()}")
                    .WithDescription(definition.Value)
                    .WithUrl(definition.Link)
                );
            }
            return Task.CompletedTask;
        }
    }
}
