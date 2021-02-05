using EventBus.Core;
using Newtonsoft.Json;

namespace EventBus.RabbitMq
{
    public class BasicJsonSerializer : IBasicSerializer
    {
        public TEvent Deserialize<TEvent>(string message) where TEvent : IEvent =>
            JsonConvert.DeserializeObject<TEvent>(message);

        public string Serialize<TEvent>(TEvent @event) =>
            JsonConvert.SerializeObject(@event);
    }
}