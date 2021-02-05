using System;
using System.Net.Http;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Routing;
using DotaPredictions.Actors;
using DotaPredictions.Actors.Predictions;
using DotaPredictions.Actors.Providers;
using DotaPredictions.Handlers;
using DotaPredictions.Models.Commands;
using EventBus.Core;
using EventBus.RabbitMq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
                return () => system.ActorOf(Props.Create(() => new PredictionManager(provider())));
            });

            services.AddScoped<IEventHandler<AddPrediction>, AddPredictionHandler>();

            services.AddSingleton<IBasicSerializer, BasicJsonSerializer>();

            services.AddRabbitMq("Test", Environment.GetEnvironmentVariable("RABBITMQ_HOST"), 
                Environment.GetEnvironmentVariable("RABBITMQ_USERNAME"), 
                Environment.GetEnvironmentVariable("RABBITMQ_PASS"));


        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRabbitMq()
                .Subscribe<AddPrediction>()
                .StartBasicConsume();
        }
    }
}
