using Ditto.Bot.Database.Models;
using Ditto.Data.Database;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Ditto.Bot.Database.Repositories.Interfaces
{
    public interface ILinkRepository :IRepository<Link>
    {
        Link GetWithLinks(int id);
        Link GetWithLinks(Expression<Func<Link, bool>> predicate);
        IEnumerable<Link> GetAllWithLinks();
        IEnumerable<Link> GetAllWithLinks(Expression<Func<Link, bool>> predicate);
    }
}
