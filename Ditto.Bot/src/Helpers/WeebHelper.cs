using Discord;
using Ditto.Bot.Data.API.Rest;
using MessageType = Ditto.Bot.Data.API.Rest.RamMoeApi.Type;

namespace Ditto.Bot.Helpers
{
    public static class WeebHelper
    {
        public static string GetImage(MessageType type)
        {
            var result = new RamMoeApi().RandomImage(type, type == MessageType.Nsfw);
            if (result == null || string.IsNullOrEmpty(result.Path))
                return null;

            return string.IsNullOrEmpty(result.Path) ? null : (result?.Path);
        }

        public static Embed GetEmbed(MessageType type, IUser user, IUser targetUser = null)
            => GetImage(type) is string imagePath
                ? new EmbedBuilder()
                    .WithDescription(FormatMessage(type, user, targetUser))
                    .WithImageUrl(imagePath).Build()
                : null;


        public static string FormatMessage(MessageType type, IUser source, IUser target = null)
        {
            switch (type)
            {
                case MessageType.Cry:
                    return target == null
                        ? source.Mention
                        : $"{source.Mention} is crying at {target.Mention} :(";
                case MessageType.Cuddle:
                    return target == null
                        ? source.Mention
                        : $"{source.Mention} is cuddling with {target.Mention}";
                case MessageType.Hug:
                    return target == null
                        ? source.Mention
                        : $"{source.Mention} hugs {target.Mention}";
                case MessageType.Kiss:
                    return target == null
                        ? source.Mention
                        : $"{source.Mention} kisses {target.Mention}!";
                case MessageType.Lewd:
                    return target == null
                        ? source.Mention
                        : $"{target.Mention} received a lewd from {source.Mention}.";
                case MessageType.Lick:
                    return target == null
                        ? source.Mention
                        : $"{source.Mention} licks {target.Mention}.";
                case MessageType.Nom:
                    return target == null
                        ? source.Mention
                        : $"{source.Mention} noms in front of {target.Mention}.";
                case MessageType.Nyan:
                    return target == null
                        ? $"{source.Mention}"
                        : $"{target.Mention} NYAN from {source.Mention}.";
                case MessageType.Owo:
                    return target == null
                        ? $"{source.Mention} OwO what's this?"
                        : $"{source.Mention}: {target.Mention} OwO what's this?";
                case MessageType.Pat:
                    return target == null
                        ? source.Mention
                        : $"{source.Mention} pats {target.Mention}!";
                case MessageType.Pout:
                    return target == null
                        ? source.Mention
                        : $"{source.Mention} pouts at {target.Mention}.";
                case MessageType.Rem:
                    return target == null
                        ? source.Mention
                        : $"{source.Mention} pokes {target.Mention} with a Rem picture.";
                case MessageType.Slap:
                    return target == null
                        ? source.Mention
                        : $"{source.Mention} slaps {target.Mention}!";
                case MessageType.Smug:
                    return target == null
                        ? source.Mention
                        : $"{source.Mention} throws a smug face at {target.Mention}.";
                case MessageType.Stare:
                    return target == null
                        ? source.Mention
                        : $"{source.Mention} stares at {target.Mention}.";
                case MessageType.Tickle:
                    return target == null
                        ? source.Mention
                        : $"{source.Mention} tickles {target.Mention}!";
                case MessageType.Triggered:
                    return target == null
                        ? source.Mention
                        : $"{source.Mention} is triggered at {target.Mention}.";
                case MessageType.Nsfw:
                    return target == null
                        ? source.Mention
                        : $"{source.Mention}, from {target.Mention}.";
                case MessageType.Potato:
                    return target == null
                        ? source.Mention
                        : $"{source.Mention} sends {target.Mention} a potato.";
                case MessageType.Kermit:
                    return target == null
                        ? source.Mention
                        : $"{source.Mention} sends {target.Mention} a kermit.";
            }

            return source.Mention;
        }
    }
}
