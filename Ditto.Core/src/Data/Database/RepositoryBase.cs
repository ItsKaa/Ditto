using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Ditto.Data.Database
{
    public abstract class RepositoryBase<TEntity> : IRepositoryBase<TEntity>
         where TEntity : class, IBaseDbEntity
    {
        protected DbContext Context { get; private set; }
        protected DbSet<TEntity> Set { get; private set; }
        
        public RepositoryBase(DbContext dbContext)
        {
            Context = dbContext;
            Set = dbContext.Set<TEntity>();
        }

        public virtual IEnumerable<TEntity> GetAll(Expression<Func<TEntity, bool>> predicate)
        {
            return Set.Where(predicate).ToList();
        }

        public TEntity Add(TEntity entity)
        {
            var entry = Context.Entry(entity);
            if (entry.State == EntityState.Detached)
            {
                entry = Set.Add(entity);
            }
            else
            {
                entry.State = EntityState.Modified;
            }
            return entry?.Entity;
        }
        public virtual TEntity Get(Expression<Func<TEntity, bool>> predicate)
        {
            return Set.Where(predicate).FirstOrDefault();
        }
        public TEntity GetOrAdd(Expression<Func<TEntity, bool>> predicate, TEntity entity)
        {
            var item = Get(predicate);
            if (item == default(TEntity))
            {
                item = Add(entity);
            }
            return item;
        }

        public virtual IEnumerable<TEntity> GetAll()
        {
            return Set.ToList();
        }

        
        public void AddRange(IEnumerable<TEntity> entities)
            =>  Set.AddRange(entities);

        public void AddRange(params TEntity[] entities)
            => Set.AddRange(entities);

        public void Remove(TEntity entity)
        {
            var entry = Context.Entry(entity);
            if (entry.State == EntityState.Detached)
            {
                Set.Attach(entity);
            }
            Context.Entry(entity).State = EntityState.Deleted;
        }

        public void Remove(Expression<Func<TEntity, bool>> predicate)
        {
            RemoveRange(Set.Where(predicate));
        }
        public void RemoveRange(IEnumerable<TEntity> entities)
            => Set.RemoveRange(entities);

        public void RemoveRange(params TEntity[] entities)
            => Set.RemoveRange(entities);

        public void Update(TEntity entity)
            => Set.Update(entity);

        public void UpdateRange(IEnumerable<TEntity> entities)
            => Set.UpdateRange(entities);

        public void UpdateRange(params TEntity[] entities)
            => Set.UpdateRange(entities);

    }
}
