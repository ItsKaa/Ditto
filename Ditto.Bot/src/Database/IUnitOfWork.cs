using Ditto.Bot.Database.Repositories.Interfaces;
using Ditto.Data.Database;

namespace Ditto.Bot.Database
{
    public partial interface IUnitOfWork : IUnitOfWorkBase
    {
        // Concrete implementation -> IRepository<Foo>
        // Add all your repositories here:
        IConfigRepository Configs { get; }
        ICommandRepository Commands { get; }
        IModuleRepository Modules { get; }
        IReminderRepository Reminders { get; }
        IEventRepository Events { get; }
        IPlaylistRepository Playlists { get; }
        ILinkRepository Links { get; }
        IBdoStatusRepository BdoStatus { get; }
    }
}
