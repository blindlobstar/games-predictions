using DotaPredictions.Infrastructure.Predictions;
using SteamKit2;
using SteamKit2.GC.Dota.Internal;

namespace DotaPredictions.Predictions.Win
{
    public class WinPredictionLogic : IPredictionLogic<CMsgDOTAMatch, ulong>
    {
        public CheckResult Check(CMsgDOTAMatch data, ulong parameters)
        {
            var isRadiantWin = data.match_outcome == EMatchOutcome.k_EMatchOutcome_RadVictory;
            var playerIndex = data.players.FindIndex(x => x.account_id == new SteamID(parameters).AccountID);
            var result = (playerIndex < data.players.Count / 2) ^
                           isRadiantWin;
            return new CheckResult()
            {
                Result = !result,
                IsFinished = true
            };
        }
    }
}