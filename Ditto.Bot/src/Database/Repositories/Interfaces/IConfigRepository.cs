using Discord;
using Ditto.Bot.Database.Models;
using Ditto.Data.Database;

namespace Ditto.Bot.Database.Repositories.Interfaces
{
    public interface IConfigRepository : IRepository<Config>
    {
        Config GetEmbedColour(IGuild guild);
        Config GetEmbedErrorColour(IGuild guild);
        Config GetEmbedRssColour(IGuild guild);
        Config GetPrefix(IGuild guild);
        Config GetBdoMaintenanceChannel(IGuild guild);

        void SetEmbedColour(IGuild guild, Color colour);
        void SetEmbedErrorColour(IGuild guild, Color colour);
        void SetEmbedRssColour(IGuild guild, Color colour);
        void SetPrefix(IGuild guild, string prefix);
        void SetBdoMaintenanceChannel(IGuild guild, ulong channelId);
        void SetBdoMaintenanceChannel(IGuild guild, ITextChannel textChannel);
    }
}
