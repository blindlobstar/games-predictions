using System;

namespace EventBus.RabbitMq
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class QueueAttribute : Attribute
    {
        private readonly string _routingKey;

        public QueueAttribute(string routingKey)
        {
            _routingKey = routingKey;
        }

        public virtual string RoutingKey => _routingKey;
    }
}
