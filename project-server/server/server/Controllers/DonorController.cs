using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebApplication1.BLL.Interfaces;
using WebApplication1.DTOs;
using WebApplication1.Models;
using System; // הוספתי עבור Exception

[Authorize(Roles = "manager")]
[ApiController]
[Route("api/[controller]")]
public class DonorController : ControllerBase
{
    private readonly IdonorBLL donorBLL;
    private readonly ILogger<DonorController> _logger;

    public DonorController(IdonorBLL bll, ILogger<DonorController> logger)
    {
        this.donorBLL = bll;
        this._logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        try
        {
            _logger.LogInformation("API: Requesting all donors.");
            var result = await this.donorBLL.Get();
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            _logger.LogInformation("API: Requesting donor by ID: {Id}", id);
            var donor = await donorBLL.GetById(id);

            if (donor == null)
            {
                _logger.LogWarning("API: Donor with ID {Id} not found.", id);
                return NotFound(new { message = "תורם לא נמצא" });
            }

            return Ok(donor);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] DonorDto donor)
    {
        if (donor == null) return BadRequest(new { message = "לא התקבלו נתונים" });

        try
        {
            if (donor.Id == 0)
            {
                _logger.LogInformation("API: Adding a new donor.");
                await this.donorBLL.Post(donor);
            }
            else
            {
                _logger.LogInformation("API: Updating existing donor with ID: {Id}", donor.Id);
                // שמרתי על הלוגיקה המקורית שלך של יצירת ה-DTO
                var donorDto = new DonorDto
                {
                    Id = donor.Id,
                    FirstName = donor.FirstName,
                    LastName = donor.LastName,
                    Phone = donor.Phone,
                    Email = donor.Email,
                    Address = donor.Address,
                };
                await this.donorBLL.Update(donor.Id, donorDto);
            }
            return Ok(new { message = "הפעולה בוצעה בהצלחה" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API Error: Post/Update donor failed.");
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] DonorDto donor)
    {
        try
        {
            _logger.LogInformation("API: Updating donor ID: {Id}", id);
            await donorBLL.Update(id, donor);
            return Ok(new { message = "העדכון בוצע בהצלחה" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            _logger.LogInformation("API: Deleting donor ID: {Id}", id);
            await donorBLL.Delete(id);
            return Ok(new { message = "התורם נמחק בהצלחה" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("by-email")]
    public async Task<IActionResult> GetByEmail([FromQuery] string email)
    {
        try
        {
            _logger.LogInformation("API: Searching donor by email: {Email}", email);
            var donor = await donorBLL.GetByEmail(email);

            if (donor == null) return NotFound(new { message = "לא נמצא תורם עם אימייל זה" });

            return Ok(donor);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("by-name")]
    public async Task<IActionResult> GetByName([FromQuery] string name)
    {
        try
        {
            _logger.LogInformation("API: Searching donor by name: {Name}", name);
            var donor = await donorBLL.GetByName(name);

            if (donor == null) return NotFound(new { message = "לא נמצא תורם בשם זה" });

            return Ok(donor);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("by-gift")]
    public async Task<IActionResult> GetByGift([FromQuery] string name)
    {
        try
        {
            _logger.LogInformation("API: Searching donor for gift ID: {Id}", name);
            var donor = await donorBLL.GetByGift(name);

            if (donor == null) return NotFound(new { message = "לא נמצאו תורמים למתנה זו" });

            return Ok(donor);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}