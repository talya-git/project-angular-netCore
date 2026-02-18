using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using server.BLL.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebApplication1.DAL;
using WebApplication1.DTOs;
using WebApplication1.Models;
using System; // נחוץ עבור Exception

[ApiController]
[Route("api/[controller]")]
public class CustomerDetailsController : ControllerBase
{
    private readonly ICustomerDatailsBLL _customerBll;
    private readonly ILogger<CustomerDetailsController> _logger;
    private readonly LotteryContext _context;

    public CustomerDetailsController(ICustomerDatailsBLL bll, ILogger<CustomerDetailsController> logger, LotteryContext context)
    {
        _customerBll = bll;
        _logger = logger;
        _context = context;
    }

    [Authorize(Roles = "manager")]
    [HttpGet("all")]
    public async Task<IActionResult> Get()
    {
        try
        {
            _logger.LogInformation("API: Fetching all customer details.");
            var result = await this._customerBll.Get();
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "manager")]
    [HttpGet("by-gift/{giftId}")]
    public async Task<IActionResult> GetByGiftId(int giftId)
    {
        try
        {
            _logger.LogInformation("API: GetByGift called for GiftId: {GiftId}", giftId);
            var result = await this._customerBll.GetByGiftId(giftId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "user")]
    [HttpPost]
    public async Task<IActionResult> Add([FromBody] CustomerDetailsDto model)
    {
        try
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized(new { message = "משתמש לא מחובר" });

            model.CustomerId = int.Parse(userIdClaim.Value);

            await _customerBll.Add(model);
            return Ok(new { message = "הרכישה נוספה בהצלחה!" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding purchase for model");
            // כאן נתפסת שגיאת ה-Truncated ונשלחת לאנגולר כהודעה ברורה
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "manager")]
    [HttpGet("reportRevenue")]
    public async Task<ActionResult<double>> reportRevenue()
    {
        try
        {
            _logger.LogInformation("API: Revenue report requested.");
            double revenue = await _customerBll.reportRevenue();
            return Ok(revenue);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "user")]
    [HttpGet("ConfirmPurchase")]
    public async Task<IActionResult> ConfirmPurchase([FromQuery] int id)
    {
        try
        {
            _logger.LogInformation("API: Confirming purchase for ID: {Id}", id);
            await _customerBll.ConfirmPurchase(id);
            return Ok(new { message = "הרכישה אושרה" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "user")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            _logger.LogInformation("API: Deleting donor ID: {Id}", id);
            await _customerBll.Delete(id);
            return Ok(new { message = "המחיקה בוצעה" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "user")]
    [HttpGet("GetMyPurchases")]
    public async Task<IActionResult> GetMyPurchases()
    {
        try
        {
            var userName = User.Identity?.Name;
            if (string.IsNullOrEmpty(userName)) return Unauthorized();

            var customer = await _context.Customers
                                         .FirstOrDefaultAsync(c => c.UserName.Trim() == userName.Trim());

            if (customer == null) return NotFound(new { message = "הלקוח לא נמצא במערכת" });

            var results = await _customerBll.GetByCustomerId(customer.Id);
            return Ok(results);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "user")]
    [HttpGet("GetTotalAmount")]
    public async Task<ActionResult<double>> GetTotalAmount()
    {
        try
        {
            var userName = User.Identity?.Name;
            if (string.IsNullOrEmpty(userName)) return Unauthorized();

            var customer = await _context.Customers
                                         .FirstOrDefaultAsync(c => c.UserName.Trim() == userName.Trim());

            if (customer == null) return Ok(0.0);
            double total = await _customerBll.GetTotalAmount(customer.Id);
            return Ok(total);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}