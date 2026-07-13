using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace NotificationService.Messaging
{
    public class NotificationConsumer : BackgroundService
    {
        private readonly ILogger<NotificationConsumer> _logger;
        private readonly string _hostName;
        private IConnection? _connection;
        private IModel? _channel;

        public NotificationConsumer(ILogger<NotificationConsumer> logger, IConfiguration config)
        {
            _logger = logger;
            _hostName = config["RabbitMQ:Host"] ?? "rabbitmq";
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    var factory = new ConnectionFactory { HostName = _hostName };
                    _connection = factory.CreateConnection();
                    _channel = _connection.CreateModel();
                    break;
                }
                catch
                {
                    _logger.LogWarning("Waiting for RabbitMQ... attempt {Attempt}/10", i + 1);
                    await Task.Delay(5000, stoppingToken);
                }
            }

            if (_channel is null) return;

            _channel.QueueDeclare("inventory.reserved", durable: true, exclusive: false, autoDelete: false);
            _channel.QueueDeclare("inventory.rejected", durable: true, exclusive: false, autoDelete: false);

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (_, ea) =>
            {
                var body = Encoding.UTF8.GetString(ea.Body.ToArray());
                var queue = ea.RoutingKey;

                if (queue == "inventory.reserved")
                {
                    var evt = JsonSerializer.Deserialize<InventoryReservedEvent>(body);
                    if (evt is not null)
                        _logger.LogInformation(
                            "[Notification] ORDER CONFIRMED — Customer notified. OrderId={OrderId} CorrelationId={CorrelationId}",
                            evt.OrderId, evt.CorrelationId);
                }
                else if (queue == "inventory.rejected")
                {
                    var evt = JsonSerializer.Deserialize<InventoryRejectedEvent>(body);
                    if (evt is not null)
                        _logger.LogWarning(
                            "[Notification] ORDER REJECTED — Customer notified. OrderId={OrderId} Reason={Reason} CorrelationId={CorrelationId}",
                            evt.OrderId, evt.Reason, evt.CorrelationId);
                }

                _channel.BasicAck(ea.DeliveryTag, false);
            };

            _channel.BasicConsume("inventory.reserved", autoAck: false, consumer: consumer);
            _channel.BasicConsume("inventory.rejected", autoAck: false, consumer: consumer);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        public override void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
            base.Dispose();
        }
    }
}
