using System;
using Akka.Actor;
using DotaPredictions.Actors.BaseTypesActors;
using DotaPredictions.Models;
using DotaPredictions.Predictions.Win;
using DotaPredictions.Repositories.Abstraction;
using EventBus.Core;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;

namespace DotaPredictions.Actors
{
    public class PredictionManager : UntypedActor
    {
        #region messages

        public class AddPredictionRequest : PredictionBase<dynamic>
        {
            public AddPredictionRequest(string userId, ulong steamId, string predictionType,
                dynamic parameters, string predictionId)
            {
                PredictionId = predictionId;
                UserId = userId;
                SteamId = steamId;
                Enum.TryParse(predictionType, out PredictionType type);
                PredictionType = type;
                Parameters = parameters;
            }

            public new PredictionType PredictionType { get; set; }
            public string PredictionId { get; set; }

        }

        public class PredictionEnds
        {
            public PredictionEnds(string id, string userId, bool result)
            {
                Result = result;
                UserId = userId;
                Id = id;
            }

            public string Id { get; private set; }
            public string UserId { get; private set; }
            public bool Result { get; private set; }
        }

        #endregion

        private readonly IActorRef _dotaClient;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IEventBus _eventBus;

        public PredictionManager(IActorRef dotaClient, IServiceScopeFactory scopeFactory, IEventBus eventBus)
        {
            _dotaClient = dotaClient;
            _scopeFactory = scopeFactory;
            _eventBus = eventBus;
        }

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case AddPredictionRequest request:
                    OnPrediction(request);
                    break;
                case PredictionEnds request:
                    OnPredictionEnd(request);
                    break;
            }

        }

        private void OnPredictionEnd(PredictionEnds request)
        {
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IPredictionRepository>();
            repo.Delete(request.Id);
            _eventBus.Publish(new Models.Commands.PredictionEnds() { Result = request.Result, UserId = request.UserId });
        }

        private void OnPrediction(AddPredictionRequest request)
        {
            switch (request.PredictionType)
            {
                case PredictionType.Win:
                    var logic = new WinPredictionLogic();
                    var actor = Context.ActorOf(GameEnd<ulong>.Props(_dotaClient, logic));
                    actor.Tell(new GameEnd<ulong>.StartPrediction(request.SteamId, request.UserId, request.PredictionId, request.SteamId));
                    break;
            }
        }

    }
}