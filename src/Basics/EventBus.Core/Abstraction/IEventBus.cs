namespace EventBus.Core
{
    public interface IEventBus
    {
        IEventBus Subscribe<TEvent>()
            where TEvent : IEvent;
        void Publish(IEvent @event);
    }
}
