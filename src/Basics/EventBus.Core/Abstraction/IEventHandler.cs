using System.Threading.Tasks;

namespace EventBus.Core
{
    public interface IEventHandler<in TEvent> where TEvent : IEvent
    {
        Task Handle(TEvent @event);
    }
}
