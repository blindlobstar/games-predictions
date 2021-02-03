using System;
using Akka.Actor;
using DotaPredictions.Actors;
using DotaPredictions.Actors.Providers;
using DotaPredictions.Models.Steam;
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
            services.AddSingleton(sp =>
                ActorSystem.Create("Base"));

            services.AddTransient<SteamClient>();

            services.AddSingleton<DotaClientProvider>(sp =>
            {
                var system = sp.GetRequiredService<ActorSystem>();
                var client = sp.GetRequiredService<SteamClient>();
                return () => system.ActorOf(DotaClient.Props(client, Environment.GetEnvironmentVariable("STEAM_API_KEY"),
                    Environment.GetEnvironmentVariable("STEAM_USERNAME"), 
                    Environment.GetEnvironmentVariable("STEAM_PASS")), 
                    "DotaClient");
            });

        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            var provider = app.ApplicationServices.GetRequiredService<DotaClientProvider>();
            var actor = provider();
            ulong serverId = actor.Ask<ulong>(new DotaClient.ServerSteamIdRequest(76561198247086602)).Result;
            var obj = actor.Ask<RealtimeStats>(new DotaClient.GameRealTimeStatsRequest(serverId)).Result;
            var match= actor.Ask(new DotaClient.GetMatchDetailsRequest(5816702121)).Result;
        }
    }
}
