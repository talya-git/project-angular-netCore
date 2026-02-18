using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using server.BLL.Interfaces;
using server.DTO;
using WebApplication1.BLL;
using WebApplication1.BLL.Interfaces;
using WebApplication1.Models;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System; // נחוץ עבור Exception

namespace server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthBLL AuthBLL;
        private readonly IMapper _mapper;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IMapper mapper, IAuthBLL authBLL, ILogger<AuthController> logger)
        {
            _mapper = mapper;
            AuthBLL = authBLL;
            _logger = logger;
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDTO registerDto)
        {
            if (registerDto == null)
            {
                _logger.LogWarning("Register attempt with null data.");
                return BadRequest(new { message = "לא התקבלו נתונים" });
            }

            try
            {
                var customer = _mapper.Map<CustomerModel>(registerDto);
                await this.AuthBLL.Register(customer);

                _logger.LogInformation("User {UserName} registered successfully.", registerDto.UserName);
                return Ok(new { message = "ההרשמה בוצעה בהצלחה" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during registration for user {UserName}", registerDto.UserName);

                // כאן השינוי המרכזי: 
                // במקום 500 כללי, אנחנו מחזירים BadRequest עם ההודעה שכתבנו ב-BLL
                return BadRequest(new { message = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpGet("login")]
        public async Task<ActionResult<AuthDTO>> Login(string userName, string password)
        {
            try
            {
                var result = await this.AuthBLL.Login(userName, password);

                if (result == null)
                {
                    _logger.LogWarning("Login failed for user: {UserName}", userName);
                    return Unauthorized(new { message = "שם משתמש או סיסימה שגויים" });
                }

                _logger.LogInformation("User {UserName} logged in successfully.", userName);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login error for user: {UserName}", userName);
                return BadRequest(new { message = "אירעה שגיאה בתהליך ההתחברות" });
            }
        }
    }
}