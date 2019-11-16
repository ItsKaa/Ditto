using Discord;
using Discord.Commands;
using Ditto.Attributes;
using Ditto.Bot.Data;
using Ditto.Data.Commands;
using Ditto.Data.Discord;
using Ditto.Extensions;
using Ditto.Helpers;
using System.Linq;
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
            if (!string.IsNullOrEmpty(result.DirectUrl))
            {
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
            else
            {
                await Context.ApplyResultReaction(CommandResult.Failed).ConfigureAwait(false);
            }
        }
        
        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.All)]
        public async Task Define([Multiword] string query)
        {
            var result = UrbanDictionary.Define(query);
            if(result != null)
            {
                var definition = result.Definitions.FirstOrDefault();
                await Context.SendPagedMessageAsync(
                    new EmbedBuilder()
                     .WithTitle($":bookmark_tabs: {definition.Word.ToTitleCase()}")
                     .WithDescription(definition.Value)
                     .WithUrl(definition.Link),
                    (message, page) =>
                    {
                        definition = result.Definitions.ElementAt(page - 1);
                        return new EmbedBuilder()
                         .WithTitle($":bookmark_tabs: {definition.Word.ToTitleCase()}")
                         .WithDescription(definition.Value)
                         .WithUrl(definition.Link);
                    },
                    result.Definitions.Count()
                ).ConfigureAwait(false);
            }
            else
            {
                await Context.ApplyResultReaction(CommandResult.Failed).ConfigureAwait(false);
            }
        }
    }
}
