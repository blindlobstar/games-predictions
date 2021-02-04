﻿using Akka.Actor;
using DotaPredictions.Actors.Predictions;
using DotaPredictions.Models;

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
                Models.PredictionType.TryParse(predictionType, out Models.PredictionType type);
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
            var addPredictionRequest = message as AddPredictionRequest;
            var predictionType = addPredictionRequest?.PredictionType;
            switch (predictionType)
            {
                case PredictionType.Win:
                    Context.ActorOf(WinActor.Props(_dotaClient));
                    break;
            }
        }
    }
}