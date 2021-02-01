using Data.MongoDB.Models;
using MongoDB.Driver;

namespace Data.MongoDB
{
    public interface IBaseContext<TEntity> where TEntity : class
    {
        IDatabaseSettings DatabaseSettings { get; }
        IMongoCollection<TEntity> GetCollection(string name);
    }
}