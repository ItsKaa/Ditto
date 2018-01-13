using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Ditto.Data.Database
{
    public abstract class Repository<TEntity> : RepositoryBase<TEntity>, IRepository<TEntity>
        where TEntity : class, IDbEntity
    {
        public Repository(DbContext dbContext) : base(dbContext)
        {
        }

        public virtual TEntity Get(int id)
        {
            return Set.FirstOrDefault(e => e.Id == id);
        }

        public void Remove(int id)
        {
            Remove(e => e.Id == id);
        }
    }
}
