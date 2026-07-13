using OrdersService.Data;
using OrdersService.Messaging;
using OrdersService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace OrdersService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly OrdersDbContext _context;
        private readonly RabbitMqPublisher _publisher;

        public OrdersController(OrdersDbContext context, RabbitMqPublisher publisher)
        {
            _context = context;
            _publisher = publisher;
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] Order order)
        {
            order.Status = "Pending";
            order.CorrelationId = Guid.NewGuid().ToString();
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            var evt = new OrderPlacedEvent(
                order.Id,
                order.CorrelationId,
                order.Items.Select(i => new OrderItemDto(i.GiftId, i.Quantity)).ToList()
            );
            _publisher.PublishOrderPlaced(evt);

            Console.WriteLine($"[Saga] Order {order.Id} created with Status=Pending. CorrelationId={order.CorrelationId}");
            return Accepted(new { Message = "Order received, processing...", OrderId = order.Id, CorrelationId = order.CorrelationId });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrder(int id)
        {
            var order = await _context.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
            if (order is null) return NotFound();
            return Ok(order);
        }
    }
}
