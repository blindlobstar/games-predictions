using System;
using System.Net.Http;
using Akka.Actor;
using Akka.Routing;
using Data.MongoDB;
using Data.MongoDB.Repositories;
using DotaPredictions.Actors;
using DotaPredictions.Actors.Providers;
using DotaPredictions.Handlers;
using DotaPredictions.Infrastructure.Contexts;
using DotaPredictions.Models.Commands;
using DotaPredictions.Models.Dto;
using DotaPredictions.Repositories.Abstraction;
using DotaPredictions.Repositories.Implementation;
using EventBus.Core;
using EventBus.RabbitMq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using SteamKit2;

namespace DotaPredictions
{
    public class Startup
    {

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(l => 
                l.AddConsole());
            services.AddSingleton(_ =>
                ActorSystem.Create("Base"));

            services.AddTransient<SteamClient>();

            services.AddHttpClient();

            services.AddTransient<IBaseContext<PredictionDto>, PredictionContext>();

            services.AddMongoDb(Environment.GetEnvironmentVariable("MONGODB_COLLECTION"),
                Environment.GetEnvironmentVariable("MONGODB_DB_NAME"),
                Environment.GetEnvironmentVariable("MONGODB_CONNECTION"));

            services.AddScoped<IPredictionRepository, PredictionRepository>();

            services.AddScoped<IEventHandler<AddPrediction>, AddPredictionHandler>();

            services.AddSingleton<IBasicSerializer, BasicJsonSerializer>();

            services.AddRabbitMq("Test", Environment.GetEnvironmentVariable("RABBITMQ_HOST"), 
                Environment.GetEnvironmentVariable("RABBITMQ_USERNAME"), 
                Environment.GetEnvironmentVariable("RABBITMQ_PASS"));


            services.AddSingleton<DotaClientProvider>(sp =>
            {
                var system = sp.GetRequiredService<ActorSystem>();
                var client = sp.GetRequiredService<SteamClient>();
                var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
                var dotaClient = system.ActorOf(DotaClient.Props(client,
                        httpClient,
                        Environment.GetEnvironmentVariable("STEAM_API_KEY"),
                        Environment.GetEnvironmentVariable("STEAM_USERNAME"),
                        Environment.GetEnvironmentVariable("STEAM_PASS")),
                    $"DotaClient-{Environment.GetEnvironmentVariable("STEAM_USERNAME")}");
                var strategy = new OneForOneStrategy(10, TimeSpan.FromSeconds(30), _ => Directive.Restart);

                return () => system.ActorOf(Props.Empty.WithRouter(new RoundRobinGroup(dotaClient.Path.ToString()))
                    .WithSupervisorStrategy(strategy));
            });

            services.AddSingleton<PredictionManagerProvider>(sp =>
            {
                var system = sp.GetRequiredService<ActorSystem>();
                var provider = sp.GetRequiredService<DotaClientProvider>();
                var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
                var eventBus = sp.GetRequiredService<IEventBus>();
                return () => system.ActorOf(Props.Create(() => new PredictionManager(provider(),
                    scopeFactory,
                    eventBus)));
            });


        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRabbitMq()
                .Subscribe<AddPrediction>()
                .StartBasicConsume();
        }
    }
}
