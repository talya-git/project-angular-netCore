using AuthService.Data;
using AuthService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthDbContext _context;

        public AuthController(AuthDbContext context)
        {
            _context = context;
        }

        // POST: api/Auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                return BadRequest("User already exists!");

            // שמירה ישירה של הסיסמה ללא Hash
            var user = new User
            {
                Name = request.Name,
                Email = request.Email,
                Password = request.Password // כאן שונה ל-Password
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "User registered successfully!" });
        }

        // POST: api/Auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            // השוואה מול השדה 'Password' שתואם למה שנשמר בהרשמה
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email && u.Password == request.Password);
            
            if (user == null) 
            {
                return Unauthorized("Invalid email or password.");
            }

            var token = $"dummy-jwt-token-for-{user.Email}-role-{user.Role}";
            return Ok(new { Token = token, Username = user.Name, Role = user.Role });
        }
    }

    public class RegisterRequest { public string Name { get; set; } = ""; public string Email { get; set; } = ""; public string Password { get; set; } = ""; }
    public class LoginRequest { public string Email { get; set; } = ""; public string Password { get; set; } = ""; }
}