using System;
using Akka.Actor;
using DotaPredictions.Actors.BaseTypesActors;
using DotaPredictions.Models;
using DotaPredictions.Predictions.Win;

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

        #endregion

        private readonly IActorRef _dotaClient;

        public PredictionManager(IActorRef dotaClient)
        {
            _dotaClient = dotaClient;
        }

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case AddPredictionRequest request:
                    OnPrediction(request);
                    break;
            }

        }

        private void OnPrediction(AddPredictionRequest request)
        {
            switch (request.PredictionType)
            {
                case PredictionType.Win:
                    var logic = new WinPredictionLogic();
                    var actor = Context.ActorOf(GameEnd<ulong>.Props(_dotaClient, logic));
                    actor.Tell(new GameEnd<ulong>.StartPrediction(request.SteamId, request.UserId, request.SteamId));
                    break;
            }
        }

    }
}