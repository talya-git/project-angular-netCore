using GiftsService.Data;
using GiftsService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiftsService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GiftsController : ControllerBase
    {
        private readonly GiftsDbContext _context;

        public GiftsController(GiftsDbContext context)
        {
            _context = context;
        }

        // GET: api/Gifts
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Gift>>> GetGifts()
        {
            return await _context.Gifts.Include(g => g.Donor).ToListAsync();
        }

        // POST: api/Gifts
        [HttpPost]
        public async Task<ActionResult<Gift>> CreateGift(Gift gift)
        {
            _context.Gifts.Add(gift);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetGifts), new { id = gift.Id }, gift);
        }
    }
}