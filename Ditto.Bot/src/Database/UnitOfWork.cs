using Ditto.Bot.Database.Repositories;
using Ditto.Bot.Database.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Ditto.Bot.Database
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly DbContext _dbContext;

        // Repositories:
        private IConfigRepository _configRepository;
        public IConfigRepository Configs => _configRepository;

        private ICommandRepository _commandRepository;
        public ICommandRepository Commands => _commandRepository;

        private IModuleRepository _moduleRepository;
        public IModuleRepository Modules => _moduleRepository;

        private IReminderRepository _reminderRepository;
        public IReminderRepository Reminders => _reminderRepository;

        private IPlaylistRepository _playlistRepository;
        public IPlaylistRepository Playlists => _playlistRepository;
        
        private ILinkRepository _links;
        public ILinkRepository Links => _links;
        
        private IBdoStatusRepository _bdoStatus;
        public IBdoStatusRepository BdoStatus => _bdoStatus;
        
        public UnitOfWork(DbContext dbContext)
        {
            _dbContext = dbContext;

            // Each repo will share the db context:
            _configRepository = new ConfigRepository(_dbContext);
            _commandRepository = new CommandRepository(_dbContext);
            _moduleRepository = new ModuleRepository(_dbContext);
            _reminderRepository = new ReminderRepository(_dbContext);
            _playlistRepository = new PlaylistRepository(_dbContext);
            _links = new LinkRepository(_dbContext);
            _bdoStatus = new BdoStatusRepository(_dbContext);
        }
        

        public int Complete()
            => _dbContext.SaveChanges();

        public Task<int> CompleteAsync(CancellationToken cancellationToken = default(CancellationToken))
            => _dbContext.SaveChangesAsync(cancellationToken);

        public void Dispose()
        {
            _dbContext.Dispose();
        }
    }
}
