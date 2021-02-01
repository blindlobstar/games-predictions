using Data.MongoDB.Models;
using MongoDB.Driver;

namespace Data.MongoDB
{
    public abstract class BaseContext<TEntity> : IBaseContext<TEntity>
        where TEntity : class
    {
        private readonly IMongoDatabase _database;
        public IDatabaseSettings DatabaseSettings { get; protected set; }

        protected BaseContext(IDatabaseSettings databaseSettings)
        {
            DatabaseSettings = databaseSettings;
            var mongoClient = new MongoClient(databaseSettings.ConnectionString);
            _database = mongoClient.GetDatabase(databaseSettings.DatabaseName);
        }

        public virtual IMongoCollection<TEntity> GetCollection(string name) =>
            _database.GetCollection<TEntity>(name);
    }
}