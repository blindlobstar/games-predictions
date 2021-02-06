using Data.MongoDB.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Data.MongoDB
{
    public static class Extensions
    {
        public static void AddMongoDb(this IServiceCollection services)
        {
            services.AddSingleton<IDatabaseSettings, DatabaseSettings>(opt =>
            {
                var configuration = opt.GetRequiredService<IConfiguration>();
                var options = new DatabaseSettings();
                configuration.GetSection("MongoDb").Bind(options);
                return options;
            });
        }

        public static void AddMongoDb(this IServiceCollection services, string collectionName, string databaseName, string connectionString)
        {
            services.AddSingleton<IDatabaseSettings, DatabaseSettings>(opt =>
            {
                var options = new DatabaseSettings()
                {
                    CollectionName = collectionName,
                    ConnectionString = connectionString,
                    DatabaseName = databaseName
                };
                return options;
            });
        }

    }
}