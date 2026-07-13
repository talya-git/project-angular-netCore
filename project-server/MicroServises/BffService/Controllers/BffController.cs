using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;

namespace BffService.Controllers
{
    [ApiController]
    [Route("api/bff")]
    public class BffController : ControllerBase
    {
        private readonly IHttpClientFactory _clientFactory;

        public BffController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        // Aggregates order data + gift details in one call
        [HttpGet("order-details/{orderId}")]
        public async Task<IActionResult> GetOrderDetails(int orderId)
        {
            var ordersClient = _clientFactory.CreateClient("OrdersClient");
            var giftsClient = _clientFactory.CreateClient("GiftsClient");

            var order = await ordersClient.GetFromJsonAsync<dynamic>($"api/Orders/{orderId}");
            if (order is null) return NotFound($"Order {orderId} not found");

            var gifts = await giftsClient.GetFromJsonAsync<dynamic[]>("api/Gifts");

            return Ok(new
            {
                Order = order,
                Gifts = gifts,
                ServedBy = Environment.GetEnvironmentVariable("HOSTNAME")
            });
        }

        // Returns container ID — proves load balancing is working
        [HttpGet("ping")]
        public IActionResult Ping() => Ok(new
        {
            Service = "BFF",
            ContainerId = Environment.GetEnvironmentVariable("HOSTNAME"),
            Timestamp = DateTime.UtcNow
        });
    }
}
