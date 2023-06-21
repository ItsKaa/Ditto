using Discord;
using Discord.Interactions;
using Ditto.Bot.Helpers;
using Ditto.Bot.Services;
using System;
using System.Threading.Tasks;
using MessageType = Ditto.Bot.Data.API.Rest.RamMoeApi.Type;

namespace Ditto.Bot.Modules.Utility
{
    public class WeebSlash : DiscordSlashModule
    {
        public WeebSlash(InteractionService interactionService, DatabaseCacheService cache, DatabaseService database) : base(interactionService, cache, database)
        {
        }

        [SlashCommand("weeb", "Sends a random weeb image.")]
        public async Task Weeb(
            [Summary("type", "The type of image")]
            MessageType messageType,
            [Summary("user", description: "The user to target, optional.")]
            IUser targetUser = null)
        {
            var ephemeral = false;
            if (messageType == MessageType.Nsfw && Context.Channel is ITextChannel textChannel && !textChannel.IsNsfw)
            {
                ephemeral = true;
            }

            await DeferAsync(ephemeral);
            var embed = WeebHelper.GetEmbed(messageType, Context.User, targetUser);
            if (embed == null)
            {
                throw new Exception("Failed to retrieve the image, something might be wrong with the API.");
            }

            await FollowupAsync(embeds: new[] { embed });
        }
    }
}
