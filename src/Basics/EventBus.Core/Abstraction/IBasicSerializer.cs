namespace EventBus.Core
{
    public interface IBasicSerializer
    {
        TEvent Deserialize<TEvent>(string message) 
            where TEvent : IEvent;

        string Serialize<TEvent>(TEvent @event);
    }
}
