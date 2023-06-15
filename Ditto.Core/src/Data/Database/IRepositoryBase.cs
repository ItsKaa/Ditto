using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Ditto.Data.Database
{
    public interface IRepositoryBase<TEntity>
        where TEntity : class, IBaseDbEntity
    {
        TEntity Get(Expression<Func<TEntity, bool>> predicate);
        IEnumerable<TEntity> GetAll();
        IEnumerable<TEntity> GetAll(Expression<Func<TEntity, bool>> predicate);

        TEntity Add(TEntity entity);
        TEntity GetOrAdd(Expression<Func<TEntity, bool>> predicate, TEntity entity);

        void AddRange(IEnumerable<TEntity> entities);
        void AddRange(params TEntity[] entities);

        void Update(TEntity entity);
        void UpdateRange(IEnumerable<TEntity> entities);
        void UpdateRange(params TEntity[] entities);

        void Remove(Expression<Func<TEntity, bool>> predicate);
        void Remove(TEntity entity);
        void RemoveRange(IEnumerable<TEntity> entities);
        void RemoveRange(params TEntity[] entities);
    }
}
