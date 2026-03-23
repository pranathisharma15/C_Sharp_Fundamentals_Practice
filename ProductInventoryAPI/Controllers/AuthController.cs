using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _config;

    public AuthController(IConfiguration config) => _config = config;

    // Demo users (later you can move this to DB)
    private readonly List<(string Username, string Password, string Role)> users = new()
    {
        ("admin", "admin123", "Admin"),
        ("manager", "manager123", "Manager"),
        ("viewer", "viewer123", "Viewer")
    };

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginModel model)
    {
        var user = users.FirstOrDefault(u =>
            u.Username.Equals(model.Username, StringComparison.OrdinalIgnoreCase) &&
            u.Password == model.Password);

        if (user == default) return Unauthorized("Invalid username or password");

        var token = GenerateJwtToken(user.Username, user.Role);
        return Ok(new { token });
    }

    private string GenerateJwtToken(string username, string role)
    {
        var jwt = _config.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role) // standard role claim
        };

        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(int.Parse(jwt["ExpiresInMinutes"]!)),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public class LoginModel
    {
        public string Username { get; set; } = default!;
        public string Password { get; set; } = default!;
    }
}