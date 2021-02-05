using EventBus.Core;

namespace DotaPredictions.Models.Commands
{
    public class PredictionEnds : IEvent
    {
        public string UserId { get; set; }
        public bool Result { get; set; }
    }
}