using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly GoogleSheetsService _googleSheetsService;

    public AuthController(IConfiguration configuration, GoogleSheetsService googleSheetsService)
    {
        _configuration = configuration;
        _googleSheetsService = googleSheetsService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (request == null || string.IsNullOrEmpty(request.UserName) || string.IsNullOrEmpty(request.Password))
        {
            return BadRequest("Invalid login request.");
        }

        // Fetch users from Google Sheets
        var users = await _googleSheetsService.GetUsersAsync();

        // Validate user credentials
        var user = users.FirstOrDefault(u =>
            u.UserName == request.UserName && u.Password == request.Password);

        if (user == null)
        {
            return Unauthorized("Invalid email or password.");
        }

        // Generate a JWT token
        var token = GenerateJwtToken(user.UserName);

        return Ok(new LoginResponse
        {
            Token = token,
            UserName = user.UserName,
            Password = user.Password // Avoid returning passwords in production
        });
    }

private string GenerateJwtToken(string email)
{
    var keyBytes = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]);
    if (keyBytes.Length < 32)
    {
        throw new ArgumentException("The JWT key must be at least 32 characters (256 bits) long.");
    }

    var securityKey = new SymmetricSecurityKey(keyBytes);
    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

    var claims = new[]
    {
        new Claim(ClaimTypes.Email, email),
        new Claim(ClaimTypes.Name, email)
    };

    var token = new JwtSecurityToken(
        issuer: _configuration["Jwt:Issuer"],
        audience: _configuration["Jwt:Audience"],
        claims: claims,
        expires: DateTime.Now.AddHours(1),
        signingCredentials: credentials);

    return new JwtSecurityTokenHandler().WriteToken(token);
}

}
