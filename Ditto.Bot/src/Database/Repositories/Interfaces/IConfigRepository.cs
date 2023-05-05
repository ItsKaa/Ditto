using Discord;
using Ditto.Bot.Database.Models;
using Ditto.Data.Database;

namespace Ditto.Bot.Database.Repositories.Interfaces
{
    public interface IConfigRepository : IRepository<Config>
    {
        Config GetGlobalCacheChannel();
        Config GetEmbedColour(IGuild guild);
        Config GetEmbedErrorColour(IGuild guild);
        Config GetEmbedRssColour(IGuild guild);
        Config GetEmbedDiscordLinkColour(IGuild guild);
        Config GetEmbedTwitchLinkColour(IGuild guild);
        Config GetEmbedMusicPlayingColour(IGuild guild);
        Config GetEmbedMusicPausedColour(IGuild guild);
        Config GetPrefix(IGuild guild);
        Config GetBdoMaintenanceChannel(IGuild guild);
        Config GetBdoNewsIdentifier(IGuild guild);
        Config GetPixivMentionRole(IGuild guild);

        void SetGlobalCacheChannel(ITextChannel textChannel);
        void SetEmbedColour(IGuild guild, Color colour);
        void SetEmbedErrorColour(IGuild guild, Color colour);
        void SetEmbedRssColour(IGuild guild, Color colour);
        void SetEmbedMusicPlayingColour(IGuild guild, Color colour);
        void SetMusicPausedColour(IGuild guild, Color colour);

        void SetPrefix(IGuild guild, string prefix);
        void SetBdoMaintenanceChannel(IGuild guild, ulong channelId);
        void SetBdoMaintenanceChannel(IGuild guild, ITextChannel textChannel);
        void SetBdoNewsIdentifier(IGuild guild, ulong identifier);
        void SetPixivMentionRole(IGuild guild, IRole role);
    }
}
