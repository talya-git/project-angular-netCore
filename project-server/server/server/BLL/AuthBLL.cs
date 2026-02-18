using BCrypt.Net;
using server.Auth.Jwt;
using server.BLL.Interfaces;
using server.DAL.Interfaces;
using server.DTO;
using System.Security.Claims;
using WebApplication1.Models; 

public class AuthBLL : IAuthBLL
{
    private readonly IAuthDAL authDAL;
    private readonly JwtTokenGenerator jwtTokenGenerator;
    private readonly ILogger<AuthBLL> _logger;

    public AuthBLL(IAuthDAL authDAL, JwtTokenGenerator jwtTokenGenerator, ILogger<AuthBLL> logger)
    {
        this.authDAL = authDAL;
        this.jwtTokenGenerator = jwtTokenGenerator;
        this._logger = logger;
    }

    public async Task Register(CustomerModel customerModel)
    {
        _logger.LogInformation("Registering user: {UserName}", customerModel.UserName);

        if (customerModel.UserName.Length > 50)
            throw new ArgumentException("שם משתמש ארוך מדי (מקסימום 50 תווים)");

        if (string.IsNullOrEmpty(customerModel.Password) || customerModel.Password.Length < 6)
            throw new ArgumentException("הסיסמה חייבת להכיל לפחות 6 תווים");

        customerModel.Password = BCrypt.Net.BCrypt.HashPassword(customerModel.Password);


        if (customerModel.Password.Length > 100)
            _logger.LogWarning("The hashed password is very long: {Length}", customerModel.Password.Length);

        try
        {
            await authDAL.Register(customerModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while saving user to DB");
            throw; 
        }
    }

    public async Task<AuthDTO> Login(string userName, string password)
    {
        _logger.LogInformation("Login attempt: {UserName}", userName);

        var user = await authDAL.GetUserByName(userName);

        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.Password))
        {
            _logger.LogWarning("Invalid login for: {UserName}", userName);
            return null; 
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
        };

        var token = jwtTokenGenerator.GenerateToken(claims);

        return new AuthDTO
        {
            Token = token,
            Role = user.Role
        };
    }
}