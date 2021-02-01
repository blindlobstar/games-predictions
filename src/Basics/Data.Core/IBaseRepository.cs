using System.Collections.Generic;
using System.Threading.Tasks;

namespace Data.Core
{
    public interface IBaseRepository<TEntity, in TKey> where TEntity : class
    {
        Task<List<TEntity>> GetAll();
        Task<TEntity> Get(TKey id);
        void Update(TEntity entity);
        Task Add(TEntity entity);
        void Delete(TEntity entity);
        Task Delete(TKey id);
    }
}