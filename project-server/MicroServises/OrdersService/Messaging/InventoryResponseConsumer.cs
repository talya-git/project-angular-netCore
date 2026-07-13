using Microsoft.EntityFrameworkCore;
using OrdersService.Data;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace OrdersService.Messaging
{
    public class InventoryResponseConsumer : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly string _hostName;
        private IConnection? _connection;
        private IModel? _channel;

        public InventoryResponseConsumer(IServiceScopeFactory scopeFactory, IConfiguration config)
        {
            _scopeFactory = scopeFactory;
            _hostName = config["RabbitMQ:Host"] ?? "rabbitmq";
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Wait for RabbitMQ to be ready
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
                    Console.WriteLine($"[OrdersService] Waiting for RabbitMQ... attempt {i + 1}/10");
                    await Task.Delay(5000, stoppingToken);
                }
            }

            if (_channel is null) return;

            _channel.QueueDeclare("inventory.reserved", durable: true, exclusive: false, autoDelete: false);
            _channel.QueueDeclare("inventory.rejected", durable: true, exclusive: false, autoDelete: false);

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (_, ea) =>
            {
                var body = Encoding.UTF8.GetString(ea.Body.ToArray());
                var queueName = ea.RoutingKey;
                Console.WriteLine($"[OrdersService] Received from {queueName}: {body}");

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();

                if (queueName == "inventory.reserved")
                {
                    var evt = JsonSerializer.Deserialize<InventoryReservedEvent>(body);
                    if (evt is null) return;
                    var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == evt.OrderId);
                    if (order is not null)
                    {
                        order.Status = "Confirmed";
                        await db.SaveChangesAsync();
                        Console.WriteLine($"[Saga] Order {evt.OrderId} CONFIRMED. CorrelationId={evt.CorrelationId}");
                    }
                }
                else if (queueName == "inventory.rejected")
                {
                    var evt = JsonSerializer.Deserialize<InventoryRejectedEvent>(body);
                    if (evt is null) return;
                    var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == evt.OrderId);
                    if (order is not null)
                    {
                        order.Status = "Cancelled";
                        await db.SaveChangesAsync();
                        Console.WriteLine($"[Saga] Order {evt.OrderId} CANCELLED (compensation). Reason={evt.Reason} CorrelationId={evt.CorrelationId}");
                    }
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
