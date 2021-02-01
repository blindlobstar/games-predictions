using RabbitMQ.Client;
using System;

namespace EventBus.RabbitMq
{
    public interface IRabbitMQPersistentConnection : IDisposable
    {
        bool IsConnected { get; }

        bool TryConnect();

        IModel CreateModel();
    }
}
