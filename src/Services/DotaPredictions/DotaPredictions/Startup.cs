using System;
using System.Net.Http;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Routing;
using DotaPredictions.Actors;
using DotaPredictions.Actors.Predictions;
using DotaPredictions.Actors.Providers;
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

        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            var logger = app.ApplicationServices.GetRequiredService<ILogger<Startup>>();
            var provider = app.ApplicationServices.GetRequiredService<DotaClientProvider>();
            var actor = provider();
            //var serverId = actor.Ask<ulong>(new DotaClient.ServerSteamIdRequest(76561198174349605)).Result;
            //var obj = actor.Ask<RealtimeStats>(new DotaClient.GameRealTimeStatsRequest(serverId)).Result;
            //var match = actor.Ask<CMsgDOTAMatch>(new DotaClient.GetMatchDetailsRequest((ulong)obj.Match.Matchid)).Result;
            var system = app.ApplicationServices.GetRequiredService<ActorSystem>();
            var winActor = system.ActorOf(Props.Create(() => new WinPrediction(actor)));
            var winActor2 = system.ActorOf(Props.Create(() => new WinPrediction(actor)));
            var winActor3 = system.ActorOf(Props.Create(() => new WinPrediction(actor)));
            var resultTask =
                winActor.Ask<WinPrediction.PredictionEnds>(new WinPrediction.StartPrediction(76561199005920395, "nor"));
            var resultTask2 =
                winActor2.Ask<WinPrediction.PredictionEnds>(
                    new WinPrediction.StartPrediction(76561198036856312, "nor"));
            var resultTask3 =
                winActor3.Ask<WinPrediction.PredictionEnds>(
                    new WinPrediction.StartPrediction(76561198177014315, "nor"));
            Task.WaitAll(resultTask, resultTask2, resultTask3);
            var result = resultTask.Result;
            logger.LogInformation($"{result}");
        }
    }
}
