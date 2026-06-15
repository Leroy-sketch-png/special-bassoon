using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace MoePortal.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class CitizenAuthController : ControllerBase
{
    private readonly IConfiguration _config;

    public CitizenAuthController(IConfiguration config)
    {
        _config = config;
    }

    [HttpPost("mock-singpass-login")]
    public IActionResult MockLogin([FromBody] MockLoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nric))
            return BadRequest("NRIC is required for mock login.");

        var tokenHandler = new JwtSecurityTokenHandler();
        
        var keyPath = _config["Singpass:PrivateKeyPath"] ?? "../keys/singpass_private.pem";
        var fullKeyPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), keyPath));

        SecurityKey signingKey;
        string algorithm;

        if (System.IO.File.Exists(fullKeyPath))
        {
            var rsa = RSA.Create();
            var pemData = System.IO.File.ReadAllText(fullKeyPath);
            rsa.ImportFromPem(pemData);
            signingKey = new RsaSecurityKey(rsa);
            algorithm = SecurityAlgorithms.RsaSha256;
        }
        else
        {
            signingKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes("this-is-a-very-long-fallback-symmetric-security-key-1234567890"));
            algorithm = SecurityAlgorithms.HmacSha256;
        }

        var signingCredentials = new SigningCredentials(signingKey, algorithm);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, request.Nric),
            new Claim("roles", "Citizen") 
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = _config["Singpass:Authority"],
            Audience = _config["AllowedOrigins:Frontend"] ?? "http://localhost:3000",
            SigningCredentials = signingCredentials
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var jwtString = tokenHandler.WriteToken(token);

        return Ok(new { Token = jwtString });
    }
}

public record MockLoginRequest(string Nric);
