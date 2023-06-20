using Discord.Commands;
using Ditto.Attributes;
using Ditto.Bot.Services;
using Ditto.Data.Commands;
using Ditto.Data.Discord;
using Ditto.Extensions.Discord;
using System.Threading.Tasks;

namespace Ditto.Bot.Modules.Help
{
    /// <summary>
    /// Helper class, parses any kind of value we might want to use.
    /// </summary>
    [DiscordPriority(-1)]
    public class Helper : DiscordTextModule
    {
        private YoutubeService YoutubeService { get; }

        public Helper(DatabaseCacheService cache, DatabaseService database, YoutubeService youtubeService) : base(cache, database)
        {
            YoutubeService = youtubeService;
        }

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
            if (YoutubeService.IsValidPlaylist(value) || YoutubeService.IsValidVideo(value))
            {
                await Module<Music.Music>(Ditto.ServiceProvider).Play(value).ConfigureAwait(false);
                await Context.Message.DeleteAfterAsync();
            }
            else
            {
                // Chat bot
                if(Context.IsBotUserTagged)
                {
                    await Module<Chat.ChatText>(Ditto.ServiceProvider).Talk(value).ConfigureAwait(false);
                }
            }
        }
    }
}
