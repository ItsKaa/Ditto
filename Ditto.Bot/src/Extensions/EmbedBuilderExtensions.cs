using Discord;

namespace Ditto.Extensions
{
    public static class EmbedBuilderExtensions
    {
        public static EmbedBuilder WithOkColour(this EmbedBuilder @this, IGuild guild)
        {
            @this.Color = Bot.Ditto.Cache.EmbedColour(guild);
            return @this;
        }

        public static EmbedBuilder WithErrorColour(this EmbedBuilder @this, IGuild guild)
        {
            @this.Color = Bot.Ditto.Cache.EmbedErrorColour(guild);
            return @this;
        }
        
        public static EmbedBuilder WithRssColour(this EmbedBuilder @this, IGuild guild)
        {
            @this.Color = Bot.Ditto.Cache.EmbedRssColour(guild);
            return @this;
        }

        public static EmbedBuilder WithDiscordLinkColour(this EmbedBuilder @this, IGuild guild)
        {
            @this.Color = Bot.Ditto.Cache.EmbedDiscordLinkColour(guild);
            return @this;
        }

        public static EmbedBuilder WithTwitchColour(this EmbedBuilder @this, IGuild guild)
        {
            @this.Color = Bot.Ditto.Cache.EmbedTwitchLinkColour(guild);
            return @this;
        }

    }
}
