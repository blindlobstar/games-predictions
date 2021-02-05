using System;
using Akka.Actor;
using Akka.Event;
using DotaPredictions.Infrastructure.Predictions;
using DotaPredictions.Models;
using DotaPredictions.Models.Steam;
using SteamKit2.GC.Dota.Internal;

namespace DotaPredictions.Actors.BaseTypesActors
{
    public class GameEnd<TParams> : UntypedActor, IWithTimers
    {
        #region messages

        public class StartPrediction : PredictionBase<TParams>
        {
            public StartPrediction(ulong steamId, string userId, TParams @params)
            {
                SteamId = steamId;
                UserId = userId;
                Parameters = @params;
            }
        }

        public class PredictionEnds
        {
            public PredictionEnds(bool result)
            {
                Result = result;
            }

            public bool Result { get; private set; }
        }

        #endregion

        private readonly ILoggingAdapter _log = Context.GetLogger();
        private readonly IActorRef _dotaClient;
        private readonly IPredictionLogic<CMsgDOTAMatch, TParams> _predictionLogic;
        private TParams Parameters { get; set; }
        public ITimerScheduler Timers { get; set; }
        private long MatchId { get; set; }
        private ulong SteamId { get; set; }

        public GameEnd(IActorRef dotaClient, IPredictionLogic<CMsgDOTAMatch, TParams> predictionLogic)
        {
            _dotaClient = dotaClient;
            _predictionLogic = predictionLogic;
        }

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case StartPrediction request:
                    _log.Info("Start new prediction for user with SteamId: {0}", request.SteamId);
                    SteamId = request.SteamId;
                    Parameters = request.Parameters;
                    _dotaClient.Tell(new DotaClient.ServerSteamIdRequest(request.SteamId));
                    break;
                case ulong serverSteamId:
                    _dotaClient.Tell(new DotaClient.GameRealTimeStatsRequest(serverSteamId));
                    break;
                case RealtimeStats stats:
                    MatchId = stats.Match.Matchid;
                    Timers.StartPeriodicTimer("getMatchDetails", "resend", TimeSpan.Zero, TimeSpan.FromSeconds(10));
                    break;
                case CMsgDOTAMatch match:
                    var checkResult = _predictionLogic.Check(match, Parameters);
                    Timers.Cancel("getMatchDetails");
                    Context.Parent.Tell(new PredictionEnds(checkResult.Result));
                    _log.Info("Prediction is over for user with SteamId: {0}, Result: {1}", SteamId, checkResult.Result);
                    Context.Stop(Self);
                    break;
                case DotaClient.NoMatchDetails:
                    break;
                case "resend":
                    _dotaClient.Tell(new DotaClient.GetMatchDetailsRequest((ulong)MatchId));
                    break;
            }
        }

        public static Props Props<T>(IActorRef dotaClient, IPredictionLogic<CMsgDOTAMatch, T> predictionLogic) =>
            Akka.Actor.Props.Create(() => new GameEnd<T>(dotaClient, predictionLogic));
    }
}