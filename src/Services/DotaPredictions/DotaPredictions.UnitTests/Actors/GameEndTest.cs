using Akka.TestKit.NUnit3;
using DotaPredictions.Actors;
using DotaPredictions.Actors.BaseTypesActors;
using DotaPredictions.Infrastructure.Predictions;
using DotaPredictions.Models.Steam;
using Moq;
using NUnit.Framework;
using SteamKit2.GC.Dota.Internal;

namespace DotaPredictions.UnitTests.Actors
{
    [TestFixture]
    public class GameEndTest : TestKit
    {
        [Test]
        public void StartPrediction_PredictionEnd_True()
        {
            //Assert
            var testSender = CreateTestProbe(Sys);
            var dotaClient = CreateTestProbe(Sys);
            var predictionLogicMock = new Mock<IPredictionLogic<CMsgDOTAMatch, ulong>>();
            predictionLogicMock.Setup(x => x.Check(It.IsAny<CMsgDOTAMatch>(), It.IsAny<ulong>()))
                .Returns(new CheckResult() {IsFinished = true, Result = true});
            var gameEndActor = testSender.ChildActorOf(GameEnd<ulong>.Props(dotaClient, predictionLogicMock.Object));
            var message = new GameEnd<ulong>.StartPrediction(ulong.MinValue, string.Empty, ulong.MinValue);
            var realtimeStat = new RealtimeStats()
            {
                Match = new Models.Steam.Match()
                {
                    Matchid = 333333
                }
            };

            //Act
            gameEndActor.Tell(message, testSender);
            dotaClient.ExpectMsg<DotaClient.ServerSteamIdRequest>(x => x.SteamId == ulong.MinValue);
            gameEndActor.Tell((ulong)123321, dotaClient);
            dotaClient.ExpectMsg<DotaClient.GameRealTimeStatsRequest>(x => x.SteamServerId == (ulong)123321);
            gameEndActor.Tell(realtimeStat, dotaClient);
            dotaClient.ExpectMsg<DotaClient.GetMatchDetailsRequest>(x => x.MatchId == (ulong)333333);
            gameEndActor.Tell(new CMsgDOTAMatch() {radiant_team_score = 10}, dotaClient);
            
            //Assert
            testSender.ExpectMsg<GameEnd<ulong>.PredictionEnds>(x => x.Result);
        }
    }
}