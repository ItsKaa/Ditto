using Discord;

namespace Ditto.Extensions
{
    public static class EmbedBuilderExtensions
    {
        public static EmbedBuilder WithOkColour(this EmbedBuilder @this, IGuild guild)
        {
            @this.Color = Bot.Ditto.Cache.Db.EmbedColour(guild);
            return @this;
        }

        public static EmbedBuilder WithErrorColour(this EmbedBuilder @this, IGuild guild)
        {
            @this.Color = Bot.Ditto.Cache.Db.EmbedErrorColour(guild);
            return @this;
        }
        
        public static EmbedBuilder WithRssColour(this EmbedBuilder @this, IGuild guild)
        {
            @this.Color = Bot.Ditto.Cache.Db.EmbedRssColour(guild);
            return @this;
        }

        public static EmbedBuilder WithDiscordLinkColour(this EmbedBuilder @this, IGuild guild)
        {
            @this.Color = Bot.Ditto.Cache.Db.EmbedDiscordLinkColour(guild);
            return @this;
        }

        public static EmbedBuilder WithTwitchColour(this EmbedBuilder @this, IGuild guild)
        {
            @this.Color = Bot.Ditto.Cache.Db.EmbedTwitchLinkColour(guild);
            return @this;
        }

    }
}
