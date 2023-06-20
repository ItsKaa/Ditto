using Discord;
using Discord.Commands;
using Ditto.Attributes;
using Ditto.Bot.Data.API.Rest;
using Ditto.Bot.Services;
using Ditto.Data.Commands;
using Ditto.Data.Discord;
using System.Threading.Tasks;
using MessageType = Ditto.Bot.Data.API.Rest.RamMoeApi.Type;

namespace Ditto.Bot.Modules.Utility
{
    [Alias("weeb")]
    public class WeebUtility : DiscordTextModule<Utility>
    {
        public WeebUtility(DatabaseCacheService cache, DatabaseService database) : base(cache, database)
        {
        }

        private async Task<string> GetImagePathAsync(MessageType type)
        {
            var result = new RamMoeApi().RandomImage(type, type == MessageType.Nsfw);
            if (result == null || string.IsNullOrEmpty(result.Path))
            {
                return null;
            }

            if (string.IsNullOrEmpty(result.Path))
            {
                await Context.ApplyResultReaction(CommandResult.Failed).ConfigureAwait(false);
            }

            return result?.Path;
        }

        private string FormatMessage(MessageType type, IUser source, IUser target = null)
        {
            string message = $"{source.Mention}: {type.ToString()}";
            switch (type)
            {
                case MessageType.Cry:
                    if (target == null)
                    {
                        message = $"{source.Mention} is crying! :(";
                    }
                    else
                    {
                        message = $"{source.Mention} is crying at {target.Mention}! :(";
                    }
                    break;
                case MessageType.Cuddle:
                    if(target == null)
                    {
                        message = $"{source.Mention} is cuddling someone.";
                    }
                    else
                    {
                        message = $"{source.Mention} is cuddling with {target.Mention}";
                    }
                    break;
                case MessageType.Hug:
                    if (target == null)
                    {
                        message = $"{source.Mention} is hugging someone.";
                    }
                    else
                    {
                        message = $"{source.Mention} hugs {target.Mention}";
                    }
                    break;
                case MessageType.Kiss:
                    if (target == null)
                    {
                        message = $"{source.Mention} kisses someone.";
                    }
                    else
                    {
                        message = $"{source.Mention} kisses {target.Mention}!";
                    }
                    break;
                case MessageType.Lewd:
                    if (target == null)
                    {
                        message = $"Lewd {source.Mention}.";
                    }
                    else
                    {
                        message = $"{target.Mention} received a lewd from {source.Mention}.";
                    }
                    break;
                case MessageType.Lick:
                    if (target == null)
                    {
                        message = $"{source.Mention} decides to lick a random person.";
                    }
                    else
                    {
                        message = $"{source.Mention} licks {target.Mention}.";
                    }
                    break;
                case MessageType.Nom:
                    if (target == null)
                    {
                        message = $"{source.Mention} noms!";
                    }
                    else
                    {
                        message = $"{source.Mention} noms in front of {target.Mention}.";
                    }
                    break;
                case MessageType.Nyan:
                    if (target == null)
                    {
                        message = $"{source.Mention}";
                    }
                    else
                    {
                        message = $"{target.Mention} NYAN from {source.Mention}.";
                    }
                    break;
                case MessageType.Owo:
                    if (target == null)
                    {
                        message = $"{source.Mention} OwO what's this?.";
                    }
                    else
                    {
                        message = $"{source.Mention}: {target.Mention} OwO what's this?";
                    }
                    break;
                case MessageType.Pat:
                    if (target == null)
                    {
                        message = $"{source.Mention} pats an unknown person, hello social distancing?";
                    }
                    else
                    {
                        message = $"{source.Mention} pats {target.Mention}!";
                    }
                    break;
                case MessageType.Pout:
                    if (target == null)
                    {
                        message = $"{source.Mention} pouts.";
                    }
                    else
                    {
                        message = $"{source.Mention} pouts at {target.Mention}.";
                    }
                    break;
                case MessageType.Rem:
                    if (target == null)
                    {
                        message = $"{source.Mention}";
                    }
                    else
                    {
                        message = $"{source.Mention} pokes {target.Mention} with a Rem picture.";
                    }
                    break;
                case MessageType.Slap:
                    if (target == null)
                    {
                        message = $"{source.Mention} slaps someone.";
                    }
                    else
                    {
                        message = $"{source.Mention} slaps {target.Mention}!";
                    }
                    break;
                case MessageType.Smug:
                    if (target == null)
                    {
                        message = $"{source.Mention} is feeling smug.";
                    }
                    else
                    {
                        message = $"{source.Mention} throws a smug face at {target.Mention}.";
                    }
                    break;
                case MessageType.Stare:
                    if (target == null)
                    {
                        message = $"{source.Mention} stares into the dark abyss.";
                    }
                    else
                    {
                        message = $"{source.Mention} stares at {target.Mention}.";
                    }
                    break;
                case MessageType.Tickle:
                    if (target == null)
                    {
                        message = $"{source.Mention} tickles.. someone?";
                    }
                    else
                    {
                        message = $"{source.Mention} tickles {target.Mention}!";
                    }
                    break;
                case MessageType.Triggered:
                    if (target == null)
                    {
                        message = $"{source.Mention} is feeling triggered.";
                    }
                    else
                    {
                        message = $"{source.Mention} is triggered at {target.Mention}.";
                    }
                    break;
                case MessageType.Nsfw:
                    if (target == null)
                    {
                        message = $"{source.Mention}";
                    }
                    else
                    {
                        message = $"{source.Mention}, from {target.Mention}.";
                    }
                    break;
                case MessageType.Potato:
                    if (target == null)
                    {
                        message = $"{source.Mention} potato.";
                    }
                    else
                    {
                        message = $"{source.Mention} sends {target.Mention} a potato.";
                    }
                    break;
                case MessageType.Kermit:
                    if (target == null)
                    {
                        message = $"{source.Mention}";
                    }
                    else
                    {
                        message = $"{source.Mention} sends {target.Mention} a kermit.";
                    }
                    break;
                default:
                    break;
            }

            return message;
        }

        public async Task SendBasicImage(MessageType type, IUser targetUser = null)
        {
            var imagePath = await GetImagePathAsync(type).ConfigureAwait(false);
            if (imagePath != null)
            {
                await Context.EmbedAsync(
                    new EmbedBuilder()
                    .WithDescription(FormatMessage(type, Context.User, targetUser))
                    .WithImageUrl(imagePath)
                ).ConfigureAwait(false);
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
            var imagePath = await GetImagePathAsync(MessageType.Nsfw).ConfigureAwait(false);
            if (imagePath != null)
            {
                await Context.Channel.SendMessageAsync($"||{FormatMessage(MessageType.Nsfw, Context.User, target)}\n{imagePath}||").ConfigureAwait(false);
            }
        }

    }
}