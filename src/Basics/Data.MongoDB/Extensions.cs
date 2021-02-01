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

    }
}