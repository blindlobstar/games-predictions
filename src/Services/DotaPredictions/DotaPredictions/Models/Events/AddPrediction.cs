using EventBus.Core;
using EventBus.RabbitMq;

namespace DotaPredictions.Models.Commands
{
    [Queue("AddPrediction")]
    public class AddPrediction : PredictionBase<dynamic>, IEvent { }
}