using Ditto.Bot.Database.Models;
using Ditto.Bot.Database.Repositories.Interfaces;
using Ditto.Data.Database;
using Microsoft.EntityFrameworkCore;

namespace Ditto.Bot.Database.Repositories
{
    public class LinkItemRepository : Repository<LinkItem>, ILinkItemRepository
    {
        public LinkItemRepository(DbContext dbContext) : base(dbContext)
        {
        }
    }
}
