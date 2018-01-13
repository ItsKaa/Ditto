using Ditto.Data.Database;
using Ditto.Bot.Database.Models;
using Ditto.Bot.Database.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Ditto.Bot.Database.Repositories
{
    public class CommandRepository : Repository<Command>, ICommandRepository
    {
        public CommandRepository(DbContext dbContext) : base(dbContext)
        {
        }
    }
}
