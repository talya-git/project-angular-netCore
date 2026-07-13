using GiftsService.Data;
using GiftsService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using MongoDB.Driver;
using System.Text.Json;

namespace GiftsService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GiftsController : ControllerBase
    {
        private readonly GiftsDbContext _context;
        private readonly IDistributedCache _cache;
        private const string CacheKey = "gifts:all";

        public GiftsController(GiftsDbContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Gift>>> GetGifts()
        {
            var correlationId = Request.Headers["X-Correlation-Id"].ToString();

            var cached = await _cache.GetStringAsync(CacheKey);
            if (cached is not null)
            {
                Console.WriteLine($"[Cache HIT] gifts:all CorrelationId={correlationId}");
                return Ok(JsonSerializer.Deserialize<List<Gift>>(cached));
            }

            Console.WriteLine($"[Cache MISS] gifts:all — fetching from MongoDB. CorrelationId={correlationId}");
            var gifts = await _context.Gifts.Find(_ => true).ToListAsync();
            foreach (var gift in gifts)
                gift.Donor = await _context.Donors.Find(d => d.Id == gift.DonorId).FirstOrDefaultAsync();

            var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) };
            await _cache.SetStringAsync(CacheKey, JsonSerializer.Serialize(gifts), options);

            return gifts;
        }

        [HttpPost]
        public async Task<ActionResult<Gift>> CreateGift(Gift gift)
        {
            await _context.Gifts.InsertOneAsync(gift);
            await _cache.RemoveAsync(CacheKey); // invalidate cache on write
            Console.WriteLine($"[Cache INVALIDATED] gifts:all after new gift created");
            return CreatedAtAction(nameof(GetGifts), new { id = gift.Id }, gift);
        }
    }
}
