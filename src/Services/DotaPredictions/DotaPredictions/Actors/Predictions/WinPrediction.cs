using System;
using System.Linq;
using System.Threading;
using Akka;
using Akka.Actor;
using Akka.Event;
using DotaPredictions.Models.Steam;
using SteamKit2;
using SteamKit2.GC.Dota.Internal;

namespace DotaPredictions.Actors.Predictions
{
    public class WinPrediction : UntypedActor, IWithTimers
    {
        #region messages

        public class StartPrediction
        {
            public StartPrediction(ulong steamId, string userId)
            {
                SteamId = steamId;
                UserId = userId;
            }

            public ulong SteamId { get; private set; }
            public string UserId { get; private set; }
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

        public ITimerScheduler Timers { get; set; }
        private long MatchId { get; set; }
        private ulong SteamId { get; set; }
        private int TeamId { get; set; }

        public WinPrediction(IActorRef dotaClient)
        {
            _dotaClient = dotaClient;
        }

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case StartPrediction request:
                    _log.Info("Start Win Prediction for user with SteamId: {0}", request.SteamId);
                    SteamId = request.SteamId;
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
                    var isRadiantWin = match.match_outcome == EMatchOutcome.k_EMatchOutcome_RadVictory;
                    var playerIndex = match.players.FindIndex(x => x.account_id == new SteamID(SteamId).AccountID);
                    var result = (playerIndex > match.players.Count / 2) ^
                                 isRadiantWin;
                    
                    Timers.Cancel("getMatchDetails");
                    Context.Parent.Tell(new PredictionEnds(result));

                    _log.Info("Win Prediction is over for user with SteamId: {0}, Result: {1}", SteamId, result);
                    Context.Stop(Self);
                    break;
                case DotaClient.NoMatchDetails:
                    break;
                case "resend":
                    _dotaClient.Tell(new DotaClient.GetMatchDetailsRequest((ulong)MatchId));
                    break;
            }
        }
    }
}