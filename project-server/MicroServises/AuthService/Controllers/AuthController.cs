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
                UserName = request.UserName,
                Email = request.Email,
                Password = request.Password,
                Phone = request.Phone,
                Address = request.Address
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "User registered successfully!" });
        }

        // POST: api/Auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                (u.UserName == request.UserName || u.Email == request.UserName) &&
                u.Password == request.Password);
            
            if (user == null) 
                return Unauthorized("Invalid credentials.");

            var token = $"dummy-jwt-token-for-{user.Email}-role-{user.Role}";
            return Ok(new { Token = token, Username = user.Name, Role = user.Role });
        }
    }

    public class RegisterRequest { public string Name { get; set; } = ""; public string UserName { get; set; } = ""; public string Email { get; set; } = ""; public string Password { get; set; } = ""; public string Phone { get; set; } = ""; public string Address { get; set; } = ""; }
    public class LoginRequest { public string UserName { get; set; } = ""; public string Password { get; set; } = ""; }
}