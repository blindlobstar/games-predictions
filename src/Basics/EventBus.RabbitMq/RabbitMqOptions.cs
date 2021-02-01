namespace EventBus.RabbitMq
{
    public class RabbitMqOptions
    {
        public string ConnectionString { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public int RetryCount { get; set; }
    }
}
