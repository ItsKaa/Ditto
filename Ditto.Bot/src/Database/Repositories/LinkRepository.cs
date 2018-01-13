using Ditto.Bot.Database.Models;
using Ditto.Bot.Database.Repositories.Interfaces;
using Ditto.Data.Database;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Ditto.Bot.Database.Repositories
{
    public class LinkRepository : Repository<Link>, ILinkRepository
    {
        public LinkRepository(DbContext dbContext) : base(dbContext)
        {
        }

        public Link GetWithLinks(int id)
            => Set.Include(r => r.Links).FirstOrDefault(e => e.Id == id);

        public Link GetWithLinks(Expression<Func<Link, bool>> predicate)
            => Set.Include(r => r.Links).FirstOrDefault(predicate);

        public IEnumerable<Link> GetAllWithLinks()
            => Set.Include(r => r.Links).ToList();

        public IEnumerable<Link> GetAllWithLinks(Expression<Func<Link, bool>> predicate)
            => Set.Include(r => r.Links).Where(predicate).ToList();
    }
}
