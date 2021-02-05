using System.Threading.Tasks;
using Akka.Actor;
using DotaPredictions.Actors;
using DotaPredictions.Actors.Providers;
using DotaPredictions.Models.Commands;
using EventBus.Core;

namespace DotaPredictions.Handlers
{
    public class AddPredictionHandler : IEventHandler<AddPrediction>
    {
        private readonly PredictionManagerProvider _predictionManagerProvider;
        private readonly IActorRef _predictionManager;

        public AddPredictionHandler(PredictionManagerProvider predictionManagerProvider)
        {
            _predictionManagerProvider = predictionManagerProvider;
            _predictionManager = _predictionManagerProvider();
        }

        public async Task Handle(AddPrediction @event)
        {
            var request = new PredictionManager.AddPredictionRequest(@event.UserId, @event.SteamId,
                @event.PredictionType, @event.Parameters, "abc");
            _predictionManager.Tell(request);
            
            await Task.Yield();
        }
    }
}