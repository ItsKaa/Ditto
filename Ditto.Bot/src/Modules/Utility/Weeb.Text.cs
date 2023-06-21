using Discord;
using Discord.Commands;
using Ditto.Attributes;
using Ditto.Bot.Helpers;
using Ditto.Bot.Services;
using Ditto.Data.Commands;
using System.Threading.Tasks;
using MessageType = Ditto.Bot.Data.API.Rest.RamMoeApi.Type;

namespace Ditto.Bot.Modules.Utility
{
    [Alias("weeb")]
    public class WeebText : DiscordTextModule<Utility>
    {
        public WeebText(DatabaseCacheService cache, DatabaseService database) : base(cache, database)
        {
        }

        public async Task SendBasicImage(MessageType type, IUser targetUser = null)
        {
            var embed = WeebHelper.GetEmbed(type, Context.User, targetUser);
            if (embed != null)
            {
                await Context.Channel.SendMessageAsync(embed: embed).ConfigureAwait(false);
            }
        }


        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.LocalAndParents)]
        public Task Image(MessageType type, IUser target = null)
        {
            return SendBasicImage(type, target);
        }

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.All)]
        public Task Cry(IUser target = null) => SendBasicImage(MessageType.Cry, target);

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.All)]
        public Task Cuddle(IUser target = null) => SendBasicImage(MessageType.Cuddle, target);

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.All)]
        public Task Hug(IUser target = null) => SendBasicImage(MessageType.Hug, target);

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.All)]
        public Task Kiss(IUser target = null) => SendBasicImage(MessageType.Kiss, target);

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.All)]
        public Task Lewd(IUser target = null) => SendBasicImage(MessageType.Lewd, target);

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.All)]
        public Task Lick(IUser target = null) => SendBasicImage(MessageType.Lick, target);

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.All)]
        public Task Nom(IUser target = null) => SendBasicImage(MessageType.Nom, target);

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.All)]
        public Task Nyan(IUser target = null) => SendBasicImage(MessageType.Nyan, target);

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.All)]
        public Task Owo(IUser target = null) => SendBasicImage(MessageType.Owo, target);

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.All)]
        public Task Pat(IUser target = null) => SendBasicImage(MessageType.Pat, target);

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.All)]
        public Task Pout(IUser target = null) => SendBasicImage(MessageType.Pout, target);

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.All)]
        public Task Rem(IUser target = null) => SendBasicImage(MessageType.Rem, target);

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.All)]
        public Task Slap(IUser target = null) => SendBasicImage(MessageType.Slap, target);

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.All)]
        public Task Smug(IUser target = null) => SendBasicImage(MessageType.Smug, target);

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.All)]
        public Task Stare(IUser target = null) => SendBasicImage(MessageType.Stare, target);

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.All)]
        public Task Tickle(IUser target = null) => SendBasicImage(MessageType.Tickle, target);

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.All)]
        public Task Triggered(IUser target = null) => SendBasicImage(MessageType.Triggered, target);

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.All)]
        public Task Potato(IUser target = null) => SendBasicImage(MessageType.Potato, target);

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.All)]
        public async Task Nsfw(IUser target = null)
        {
            if (WeebHelper.GetImage(MessageType.Nsfw) is string imagePath)
            {
                await Context.Channel.SendMessageAsync($"||{WeebHelper.FormatMessage(MessageType.Nsfw, Context.User, target)}\n{imagePath}||").ConfigureAwait(false);
            }
        }

    }
}