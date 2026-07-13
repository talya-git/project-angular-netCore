using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace OrdersService.Messaging
{
    public class RabbitMqPublisher : IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;

        public RabbitMqPublisher(string hostName)
        {
            var factory = new ConnectionFactory { HostName = hostName };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.QueueDeclare("order.placed", durable: true, exclusive: false, autoDelete: false);
        }

        public void PublishOrderPlaced(OrderPlacedEvent evt)
        {
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(evt));
            var props = _channel.CreateBasicProperties();
            props.Persistent = true;
            props.CorrelationId = evt.CorrelationId;
            Console.WriteLine($"[RabbitMQ] Publishing OrderPlaced: OrderId={evt.OrderId} CorrelationId={evt.CorrelationId}");
            _channel.BasicPublish("", "order.placed", props, body);
        }

        public void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
        }
    }
}
