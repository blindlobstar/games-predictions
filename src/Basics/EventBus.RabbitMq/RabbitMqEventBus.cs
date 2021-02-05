using Microsoft.Extensions.Logging;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using EventBus.Core;
using Microsoft.Extensions.DependencyInjection;

namespace EventBus.RabbitMq
{
    public class RabbitMqEventBus : IEventBus
    {
        private readonly string BROKER_NAME = "default";

        private readonly ILogger<RabbitMqEventBus> _logger;
        private readonly string _queueName;
        private readonly IRabbitMQPersistentConnection _persistentConnection;
        private readonly IBasicSerializer _basicSerializer;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly int _retryCount;

        private IModel _consumerChannel;
        private readonly Dictionary<string, Func<string, Task>> _eventToHandler;

        public RabbitMqEventBus(IRabbitMQPersistentConnection persistentConnection,
            ILogger<RabbitMqEventBus> logger,
            IBasicSerializer basicSerializer,
            IServiceScopeFactory serviceScopeFactory,
            string queueName = null,
            int retryCount = 5)
        {
            _persistentConnection = persistentConnection ?? throw new ArgumentNullException(nameof(persistentConnection));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _queueName = queueName ?? "Default";
            _consumerChannel = CreateConsumerChannel();
            _eventToHandler = new();
            _basicSerializer = basicSerializer;
            _serviceScopeFactory = serviceScopeFactory;
            _retryCount = retryCount;
        }

        public void Publish(IEvent @event)
        {
            if (!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnect();
            }

            var policy = Policy.Handle<BrokerUnreachableException>()
                .Or<SocketException>()
                .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                {
                    _logger.LogWarning(ex, "Could not publish event: {EventName} after {Timeout}s ({ExceptionMessage})", @event.GetType().Name, $"{time.TotalSeconds:n1}", ex.Message);
                });

            var queueAttribute = (QueueAttribute)Attribute.GetCustomAttribute(@event.GetType(), typeof(QueueAttribute));
            var eventName = queueAttribute == null ? @event.GetType().Name : queueAttribute.RoutingKey;

            _logger.LogTrace("Creating RabbitMQ channel to publish event: {EventName}",  eventName);

            using var channel = _persistentConnection.CreateModel();
            _logger.LogTrace("Declaring RabbitMQ exchange to publish event: {EventName}", eventName);

            channel.ExchangeDeclare(exchange: BROKER_NAME, type: "direct");

            var message = _basicSerializer.Serialize(@event);
            var body = Encoding.UTF8.GetBytes(message);

            policy.Execute(() =>
            {
                var properties = channel.CreateBasicProperties();
                properties.DeliveryMode = 2; // persistent

                _logger.LogTrace("Publishing event to RabbitMQ: {EventName}", eventName);

                channel.BasicPublish(
                    exchange: BROKER_NAME,
                    routingKey: eventName,
                    mandatory: true,
                    basicProperties: properties,
                    body: body);
            });
        }

        public IEventBus Subscribe<TEvent>()
            where TEvent : IEvent
        {
            var eventType = typeof(TEvent);
            var queueAttribute = (QueueAttribute)Attribute.GetCustomAttribute(eventType, typeof(QueueAttribute));
            var eventName = queueAttribute == null ? eventType.Name : queueAttribute.RoutingKey;

            DoInternalSubscription(eventName);

            _eventToHandler.Add(eventName, async (eventMsg) =>
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<IEventHandler<TEvent>>();
                var @event = _basicSerializer.Deserialize<TEvent>(eventMsg);
                await handler.Handle(@event);
            });

            return this;
        }

        private void DoInternalSubscription(string eventName)
        {
            if (_eventToHandler.ContainsKey(eventName)) 
                return;

            if (!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnect();
            }

            using var channel = _persistentConnection.CreateModel();
            channel.QueueBind(queue: _queueName,
                exchange: BROKER_NAME,
                routingKey: eventName);
        }

        public void StartBasicConsume(string eventName = null)
        {
            _logger.LogTrace("Starting RabbitMQ basic consume");

            if (_consumerChannel != null)
            {
                var consumer = new AsyncEventingBasicConsumer(_consumerChannel);

                consumer.Received += ConsumerRecieved;

                _consumerChannel.BasicConsume(
                    queue: _queueName,
                    autoAck: false,
                    consumer: consumer);
            }
            else
            {
                _logger.LogError("StartBasicConsume can't call on _consumerChannel == null");
            }
        }

        private async Task ConsumerRecieved(object sender, BasicDeliverEventArgs eventArgs)
        {
            var eventName = eventArgs.RoutingKey;
            var message = Encoding.UTF8.GetString(eventArgs.Body.ToArray());

            try
            {
                if (message.ToLowerInvariant().Contains("throw-fake-exception"))
                {
                    throw new InvalidOperationException($"Fake exception requested: \"{message}\"");
                }

                await ProcessEvent(eventName, message);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"----- ERROR Processing message \"{message}\"");
            }

            _consumerChannel.BasicAck(eventArgs.DeliveryTag, multiple: false);
        }

        private async Task ProcessEvent(string eventName, string message)
        {
            _logger.LogTrace($"Start process {eventName} with message: {message}");
            
            if (_eventToHandler.TryGetValue(eventName, out var handler))
                await handler(message);
            else
                _logger.LogWarning($"No subscription for RabbitMQ event: {eventName}");
        }

        private IModel CreateConsumerChannel()
        {
            if (!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnect();
            }

            _logger.LogTrace("Creating RabbitMQ consumer channel");

            var channel = _persistentConnection.CreateModel();

            channel.ExchangeDeclare(exchange: BROKER_NAME,
                                    type: "direct");

            channel.QueueDeclare(queue: _queueName,
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

            channel.CallbackException += (_, ea) =>
            {
                _logger.LogWarning(ea.Exception, "Recreating RabbitMQ consumer channel");

                _consumerChannel.Dispose();
                _consumerChannel = CreateConsumerChannel();
                StartBasicConsume();
            };

            return channel;
        }
    }
}
