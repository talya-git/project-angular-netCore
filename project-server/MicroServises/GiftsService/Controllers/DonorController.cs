using GiftsService.Data;
using GiftsService.Models;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace GiftsService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DonorController : ControllerBase
    {
        private readonly GiftsDbContext _context;

        public DonorController(GiftsDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Donor>>> GetDonors()
        {
            var donors = await _context.Donors.Find(_ => true).ToListAsync();
            return Ok(donors);
        }

        [HttpPost]
        public async Task<ActionResult<Donor>> CreateDonor([FromBody] System.Text.Json.JsonElement body)
        {
            var donor = new Donor
            {
                Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
                FirstName = body.GetProperty("firstName").GetString() ?? "",
                LastName = body.GetProperty("lastName").GetString() ?? "",
                Email = body.TryGetProperty("email", out var e) ? e.GetString() ?? "" : "",
                Phone = body.TryGetProperty("phone", out var p) ? p.GetString() ?? "" : "",
                Address = body.TryGetProperty("address", out var a) ? a.GetString() ?? "" : ""
            };
            await _context.Donors.InsertOneAsync(donor);
            return CreatedAtAction(nameof(GetDonors), new { id = donor.Id }, donor);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDonor(string id)
        {
            await _context.Donors.DeleteOneAsync(d => d.Id == id);
            return NoContent();
        }
    }
}
