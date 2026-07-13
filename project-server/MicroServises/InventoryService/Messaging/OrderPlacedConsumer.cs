using InventoryService.Data;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace InventoryService.Messaging
{
    public class OrderPlacedConsumer : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly string _hostName;
        private IConnection? _connection;
        private IModel? _channel;

        public OrderPlacedConsumer(IServiceScopeFactory scopeFactory, IConfiguration config)
        {
            _scopeFactory = scopeFactory;
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
                    Console.WriteLine($"[InventoryService] Waiting for RabbitMQ... attempt {i + 1}/10");
                    await Task.Delay(5000, stoppingToken);
                }
            }

            if (_channel is null) return;

            _channel.QueueDeclare("order.placed", durable: true, exclusive: false, autoDelete: false);
            _channel.QueueDeclare("inventory.reserved", durable: true, exclusive: false, autoDelete: false);
            _channel.QueueDeclare("inventory.rejected", durable: true, exclusive: false, autoDelete: false);

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (_, ea) =>
            {
                var body = Encoding.UTF8.GetString(ea.Body.ToArray());
                var evt = JsonSerializer.Deserialize<OrderPlacedEvent>(body);
                if (evt is null) { _channel.BasicAck(ea.DeliveryTag, false); return; }

                Console.WriteLine($"[InventoryService] Processing OrderPlaced: OrderId={evt.OrderId} CorrelationId={evt.CorrelationId}");

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

                bool allAvailable = true;
                string? rejectReason = null;

                foreach (var item in evt.Items)
                {
                    var stock = await db.Inventory.FirstOrDefaultAsync(i => i.GiftId == item.GiftId);
                    if (stock is null || stock.AvailableStock < item.Quantity)
                    {
                        allAvailable = false;
                        rejectReason = $"Gift {item.GiftId} out of stock (requested={item.Quantity}, available={stock?.AvailableStock ?? 0})";
                        break;
                    }
                }

                if (allAvailable)
                {
                    foreach (var item in evt.Items)
                    {
                        var stock = await db.Inventory.FirstOrDefaultAsync(i => i.GiftId == item.GiftId);
                        stock!.AvailableStock -= item.Quantity;
                    }
                    await db.SaveChangesAsync();

                    var reserved = JsonSerializer.Serialize(new InventoryReservedEvent(evt.OrderId, evt.CorrelationId));
                    var props = _channel.CreateBasicProperties();
                    props.CorrelationId = evt.CorrelationId;
                    _channel.BasicPublish("", "inventory.reserved", props, Encoding.UTF8.GetBytes(reserved));
                    Console.WriteLine($"[Saga] Inventory RESERVED for Order {evt.OrderId}. CorrelationId={evt.CorrelationId}");
                }
                else
                {
                    var rejected = JsonSerializer.Serialize(new InventoryRejectedEvent(evt.OrderId, evt.CorrelationId, rejectReason!));
                    var props = _channel.CreateBasicProperties();
                    props.CorrelationId = evt.CorrelationId;
                    _channel.BasicPublish("", "inventory.rejected", props, Encoding.UTF8.GetBytes(rejected));
                    Console.WriteLine($"[Saga] Inventory REJECTED for Order {evt.OrderId}. Reason={rejectReason} CorrelationId={evt.CorrelationId}");
                }

                _channel.BasicAck(ea.DeliveryTag, false);
            };

            _channel.BasicConsume("order.placed", autoAck: false, consumer: consumer);
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
