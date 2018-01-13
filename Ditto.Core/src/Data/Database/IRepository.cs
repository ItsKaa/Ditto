namespace Ditto.Data.Database
{

    public interface IRepository<TEntity> : IRepositoryBase<TEntity>
        where TEntity : class, IDbEntity
    {
        TEntity Get(int id);
        void Remove(int id);
    }
}
