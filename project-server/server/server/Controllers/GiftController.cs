using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using server.DAL;
using server.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApplication1.BLL;
using WebApplication1.BLL.Interfaces;
using WebApplication1.DTOs;
using WebApplication1.Models;

[ApiController]
[Route("api/[controller]")]
public class GiftController : ControllerBase
{
    private readonly IgiftBLL giftBLL;
    private readonly ILogger<GiftController> _logger;

    public GiftController(IgiftBLL gift, ILogger<GiftController> logger)
    {
        this.giftBLL = gift;
        this._logger = logger;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        try
        {
            _logger.LogInformation("API Request: Fetching all gifts.");
            var result = await this.giftBLL.Get();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Get Gifts");
            return BadRequest(new { message = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            _logger.LogInformation("API: Requesting gift by ID: {Id}", id);
            var gift = await giftBLL.GetById(id);

            if (gift == null)
            {
                _logger.LogWarning("API: Gift with ID {Id} not found.", id);
                return NotFound(new { message = "מתנה לא נמצאה" });
            }

            return Ok(gift);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "manager")]
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] GiftDto gift)
    {
        try
        {
            _logger.LogInformation("API Request: Creating a new gift: {GiftName}", gift?.Name);
            await giftBLL.Post(gift);
            return Ok(new { message = "המתנה נוספה בהצלחה" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating gift");
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "manager")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] GiftDto gift)
    {
        try
        {
            _logger.LogInformation("API Request: Updating gift ID: {Id}", id);
            await giftBLL.Update(id, gift);
            return Ok(new { message = "המתנה עודכנה בהצלחה" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "manager")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            _logger.LogInformation("API Request: Deleting gift ID: {Id}", id);
            await giftBLL.Delete(id);
            return Ok(new { message = "המתנה נמחקה" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpGet("by-name")]
    public async Task<IActionResult> GetByName([FromQuery] string name)
    {
        try
        {
            _logger.LogInformation("API Request: Search gift by name: {Name}", name);
            var result = await giftBLL.GetByName(name);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "manager")]
    [HttpGet("by-parches-count")]
    public async Task<IActionResult> GetByPurchasesCount([FromQuery] int num)
    {
        try
        {
            _logger.LogInformation("API Request: Search gift by parches: {num}", num);
            var result = await giftBLL.GetByPurchasesCount(num);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "manager")]
    [HttpGet("by-donor")]
    public async Task<IActionResult> GetByDonor([FromQuery] string name)
    {
        try
        {
            _logger.LogInformation("API Request: Search gift by name: {Name}", name);
            var result = await giftBLL.GetByDonor(name);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "manager")]
    [HttpPost("winner/{giftId}")]
    public async Task<IActionResult> Winner(int giftId)
    {
        try
        {
            _logger.LogInformation("API Request: Starting lottery for gift ID: {Id}", giftId);
            var winnerDto = await giftBLL.Winner(giftId);

            if (winnerDto == null)
                return BadRequest(new { message = "לא ניתן לבצע הגרלה (ייתכן שאין רכישות מאושרות או שכבר הוכרז זוכה)" });

            return Ok(winnerDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lottery failed for GiftId {Id}", giftId);
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "manager")]
    [HttpGet("reportWinners")]
    public async Task<IActionResult> reportWinners()
    {
        try
        {
            _logger.LogInformation("API Request: Generating winners report.");
            var result = await giftBLL.reportWinners();
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "manager")]
    [HttpGet("giftExpensive")]
    public async Task<IActionResult> giftExpensive()
    {
        try
        {
            _logger.LogInformation("API Request: Fetching most expensive gift.");
            var result = await giftBLL.giftExpensive();
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "manager")]
    [HttpGet("reportAchnasot")]
    public async Task<IActionResult> reportAchnasot()
    {
        try
        {
            _logger.LogInformation("API Request: Fetching revenue report.");
            var result = await this.giftBLL.reportAchnasot();
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}