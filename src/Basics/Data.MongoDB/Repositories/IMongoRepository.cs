using System.Threading.Tasks;
using Data.Core;

namespace Data.MongoDB.Repositories
{
    public interface IMongoRepository<TEntity> : IBaseRepository<TEntity, string> where TEntity : class
    {
        Task UpdateAsync(TEntity entity);
    }
}
