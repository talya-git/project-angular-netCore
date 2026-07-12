using InventoryService.Data;
using InventoryService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryController : ControllerBase
    {
        private readonly InventoryDbContext _context;

        public InventoryController(InventoryDbContext context)
        {
            _context = context;
        }

        // GET: api/Inventory/{giftId} -> בודק מלאי למתנה ספציפית
        [HttpGet("{giftId}")]
        public async Task<ActionResult<int>> GetStock(int giftId)
        {
            var item = await _context.Inventory.FirstOrDefaultAsync(i => i.GiftId == giftId);
            if (item == null) return NotFound("Gift not found in inventory.");
            return item.AvailableStock;
        }

        // POST: api/Inventory/reduce -> הפחתת מלאי (יקראו לזה בזמן הזמנה)
        [HttpPost("reduce")]
        public async Task<IActionResult> ReduceStock([FromBody] ReduceStockRequest request)
        {
            var item = await _context.Inventory.FirstOrDefaultAsync(i => i.GiftId == request.GiftId);
            if (item == null) return NotFound("Gift not found in inventory.");

            if (item.AvailableStock < request.Quantity)
            {
                return BadRequest("Not enough tickets left in stock!");
            }

            item.AvailableStock -= request.Quantity;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Stock reduced successfully", NewStock = item.AvailableStock });
        }
    }

    public class ReduceStockRequest
    {
        public int GiftId { get; set; }
        public int Quantity { get; set; }
    }
}