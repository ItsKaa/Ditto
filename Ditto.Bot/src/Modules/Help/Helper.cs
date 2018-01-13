using Discord.Commands;
using Ditto.Attributes;
using Ditto.Bot.Modules.Chat;
using Ditto.Data.Commands;
using Ditto.Data.Discord;
using System.Threading.Tasks;

namespace Ditto.Bot.Modules.Help
{
    /// <summary>
    /// Helper class, parses any kind of value we might want to use.
    /// </summary>
    [DiscordPriority(-1)]
    public class Helper : DiscordModule
    {
        [DiscordCommand(
            CommandSourceLevel.All,
            CommandAccessLevel.Global | CommandAccessLevel.Local | CommandAccessLevel.Parents,
            RequireBotTag = false,
            AcceptBotTag = true)
        ]

        [Alias("")]
        [Priority(-1)]
        public async Task _([Multiword] string value = "")
        {
            // Youtube parsing
            if (Ditto.Google.Youtube.IsValidPlaylist(value) || Ditto.Google.Youtube.IsValidVideo(value))
            {
                // play playlist/song
                await Context.EmbedAsync(
                    "Youtube not yet implemented.",
                    ContextMessageOption.ReplyUser
                ).ConfigureAwait(false);
#if TESTING
                await new Music.Music() { Context = this.Context}.Play(value);
#endif
            }
            else
            {
                if(Context.IsBotUserTagged)
                {
                    await Module<ChatModule>().Talk(value).ConfigureAwait(false);
                }
            }
        }
    }
}
