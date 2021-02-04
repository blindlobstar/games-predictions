using EventBus.Core;

namespace DotaPredictions.Models.Commands
{
    public class AddPrediction : PredictionBase<dynamic>, IEvent { }
}