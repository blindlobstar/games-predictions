using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using EventBus.Core;

namespace EventBus.RabbitMq
{
    public static class Extensions
    {
        public static IEventBus UseRabbitMq(this IApplicationBuilder app)
            => app.ApplicationServices.GetRequiredService<IEventBus>();

        public static void AddRabbitMq(this IServiceCollection services, string queueName)
        {
            services.AddSingleton(service =>
            {
                var configuration = service.GetRequiredService<IConfiguration>();
                var options = new RabbitMqOptions();
                configuration.GetSection("RabbitMq").Bind(options);
                return options;
            });

            services.AddSingleton<IRabbitMQPersistentConnection>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<DefaultRabbitMQPersistentConnection>>();
                var options = sp.GetRequiredService<RabbitMqOptions>();
                var factory = new ConnectionFactory()
                {
                    HostName = options.ConnectionString,
                    DispatchConsumersAsync = true
                };
                var retryCount = options.RetryCount == 0 ? 5
                    : options.RetryCount;
                return new DefaultRabbitMQPersistentConnection(factory, logger, retryCount);
            });

            services.AddSingleton<IEventBus>(sp => new RabbitMqEventBus(
                persistentConnection: sp.GetRequiredService<IRabbitMQPersistentConnection>(),
                logger: sp.GetRequiredService<ILogger<RabbitMqEventBus>>(),
                basicSerializer: sp.GetRequiredService<IBasicSerializer>(),
                serviceScopeFactory: sp.GetRequiredService<IServiceScopeFactory>(),
                queueName: queueName));


        }

        public static void AddRabbitMq(this IServiceCollection services, string queueName, string hostName, string login = null, 
            string password = null, int? retryCount = null)
        {
            services.AddSingleton<IRabbitMQPersistentConnection>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<DefaultRabbitMQPersistentConnection>>();
                var factory = new ConnectionFactory()
                {
                    HostName = hostName,
                    UserName = login,
                    Password = password,
                    DispatchConsumersAsync = true
                };
                var rCount = retryCount ?? 5; 
                return new DefaultRabbitMQPersistentConnection(factory, logger, rCount);
            });

            services.AddSingleton<IEventBus>(sp => new RabbitMqEventBus(
                persistentConnection: sp.GetRequiredService<IRabbitMQPersistentConnection>(),
                logger: sp.GetRequiredService<ILogger<RabbitMqEventBus>>(),
                basicSerializer: sp.GetRequiredService<IBasicSerializer>(),
                serviceScopeFactory: sp.GetRequiredService<IServiceScopeFactory>(),
                queueName: queueName));
        }
    }
}
