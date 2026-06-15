using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace MoePortal.Api.Controllers;

/// <summary>
/// Simulates the Singpass FAPI 2.0 PAR identity provider locally.
/// </summary>
[ApiController]
[Route("api/simulator/singpass")]
public class SingpassSimulatorController : ControllerBase
{
    private readonly IConfiguration _config;

    public SingpassSimulatorController(IConfiguration config)
    {
        _config = config;
    }

    [HttpPost("par")]
    public IActionResult InitiatePar([FromForm] string client_id, [FromForm] string response_type, [FromForm] string scope, [FromForm] string code_challenge)
    {
        // Simulator for PAR endpoint: return a request URI.
        var requestUri = $"urn:ietf:params:oauth:request_uri:{Guid.NewGuid()}";
        return Ok(new
        {
            request_uri = requestUri,
            expires_in = 60
        });
    }

    [HttpPost("token")]
    public IActionResult ExchangeToken([FromForm] string code, [FromForm] string code_verifier)
    {
        // Simulator for Token endpoint
        // Produce a signed JWS
        var keyPath = _config["Singpass:PrivateKeyPath"] ?? "../keys/singpass_private.pem";
        var fullKeyPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), keyPath));

        RsaSecurityKey? rsaKey = null;
        if (System.IO.File.Exists(fullKeyPath))
        {
            var rsa = RSA.Create();
            rsa.ImportFromPem(System.IO.File.ReadAllText(fullKeyPath));
            rsaKey = new RsaSecurityKey(rsa);
        }

        SecurityKey issuerKey = rsaKey != null
            ? (SecurityKey)rsaKey
            : new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes("this-is-a-very-long-fallback-symmetric-security-key-1234567890"));

        var credentials = new SigningCredentials(issuerKey, SecurityAlgorithms.HmacSha256Signature);
        if (rsaKey != null)
        {
            credentials = new SigningCredentials(rsaKey, SecurityAlgorithms.RsaSha256);
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, "S1234567A"),
                new Claim("name", "Alpha Tester"),
                new Claim("roles", "CITIZEN")
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = _config["Singpass:Authority"] ?? "https://stg-id.singpass.gov.sg",
            Audience = _config["AllowedOrigins:Frontend"] ?? "http://localhost:3000",
            SigningCredentials = credentials
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var jwt = tokenHandler.WriteToken(token);

        return Ok(new
        {
            access_token = jwt,
            id_token = jwt,
            token_type = "Bearer",
            expires_in = 3600
        });
    }

    [HttpGet("userinfo")]
    public IActionResult GetUserInfo()
    {
        return Ok(new
        {
            sub = "S1234567A",
            name = "Alpha Tester",
            birthdate = "2005-01-01",
            nationality = "SINGAPORE CITIZEN"
        });
    }
}
