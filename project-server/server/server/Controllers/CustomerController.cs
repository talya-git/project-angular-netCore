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
public class CustomerController : ControllerBase
{
    private readonly IcustomerBLL customerBLL;
    private readonly ILogger<CustomerController> _logger;

    public CustomerController(IcustomerBLL bll, ILogger<CustomerController> logger)
    {
        this.customerBLL = bll;
        this._logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<CustomerDto>>> Get()
    {
        try
        {
            _logger.LogInformation("API Request: Fetching all customers.");

            var result = await this.customerBLL.Get();

            if (result == null || result.Count == 0)
            {
                _logger.LogWarning("API Response: No customers found or list is empty.");
                // מחזירים רשימה ריקה עם סטטוס OK, זה הכי נוח לאנגולר
                return Ok(new List<CustomerDto>());
            }

            _logger.LogInformation("API Response: Returning {Count} customers.", result.Count);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API Error: Failed to fetch customers.");
            // מחזירים הודעה מובנת ללקוח
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CustomerDto>> GetById(int id)
    {
        try
        {
            _logger.LogInformation("API: Requesting customer by ID: {Id}", id);
            var c = await customerBLL.GetById(id);

            if (c == null)
            {
                _logger.LogWarning("API: Customer with ID {Id} not found.", id);
                return NotFound(new { message = $"לקוח עם מזהה {id} לא נמצא במערכת." });
            }

            return Ok(c);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API Error: Failed to fetch customer with ID {Id}", id);
            return BadRequest(new { message = ex.Message });
        }
    }
}