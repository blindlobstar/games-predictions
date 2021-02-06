using System.Threading.Tasks;
using Akka.Actor;
using DotaPredictions.Actors;
using DotaPredictions.Actors.Providers;
using DotaPredictions.Models.Commands;
using DotaPredictions.Models.Dto;
using DotaPredictions.Repositories.Abstraction;
using EventBus.Core;

namespace DotaPredictions.Handlers
{
    public class AddPredictionHandler : IEventHandler<AddPrediction>
    {
        private readonly IActorRef _predictionManager;
        private readonly IPredictionRepository _predictionRepository;

        public AddPredictionHandler(PredictionManagerProvider predictionManagerProvider, 
            IPredictionRepository predictionRepository)
        {
            _predictionRepository = predictionRepository;
            _predictionManager = predictionManagerProvider();
        }

        public async Task Handle(AddPrediction @event)
        {
            var predictionDto = new PredictionDto()
            {
                SteamId = @event.SteamId,
                IsFinished = false,
                PredictionType = @event.PredictionType,
                UserId = @event.UserId,
                Parameters = @event.Parameters
            };

            await _predictionRepository.Add(predictionDto);
            
            var request = new PredictionManager.AddPredictionRequest(@event.UserId, @event.SteamId,
                @event.PredictionType, @event.Parameters, predictionDto.Id);
            _predictionManager.Tell(request);
        }
    }
}