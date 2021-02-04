using Akka.Actor;
using DotaPredictions.Models;

namespace DotaPredictions.Actors
{
    public class PredictionManager : UntypedActor
    {
        #region messages

        public class AddPredictionRequest
        {
            public AddPredictionRequest(string userId, ulong steamId, PredictionType predictionType, object @params)
            {
                UserId = userId;
                SteamId = steamId;
                PredictionType = predictionType;
                Params = @params;
            }

            public PredictionType PredictionType { get; private set; }
            public ulong SteamId { get; private set; }
            public string UserId { get; private set; }
            public object Params { get; private set; }
        }

        #endregion

        protected override void OnReceive(object message)
        {
            throw new System.NotImplementedException();
        }
    }
}