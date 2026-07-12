using OrdersService.Data;
using OrdersService.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;

namespace OrdersService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly OrdersDbContext _context;
        private readonly IHttpClientFactory _clientFactory;

        public OrdersController(OrdersDbContext context, IHttpClientFactory clientFactory)
        {
            _context = context;
            _clientFactory = clientFactory;
        }

        // POST: api/Orders -> יצירת הזמנה חדשה ועדכון מלאי מבוזר
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] Order order)
        {
            var client = _clientFactory.CreateClient("InventoryClient");

            // 1. מעבר על כל הפריטים בהזמנה ועדכון המלאי בשירות המלאי
            foreach (var item in order.Items)
            {
                var reduceRequest = new { GiftId = item.GiftId, Quantity = item.Quantity };
                var response = await client.PostAsJsonAsync("api/Inventory/reduce", reduceRequest);

                if (!response.IsSuccessStatusCode)
                {
                    return BadRequest($"Failed to secure tickets for Gift ID {item.GiftId}. Out of stock or service unavailable.");
                }
            }

            // 2. אם כל המלאי עודכן בהצלחה - נשמור את ההזמנה אצלנו
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Order created successfully!", OrderId = order.Id });
        }
    }
}