using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using TicketFlow.API.Settings;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

public class TokenService(IOptions<JwtSettings> jwtOptions)
{
    private readonly JwtSettings _jwt = jwtOptions.Value;
    public string GenerateToken(ApplicationUser user, int? domainUserId = null, int? agentId = null)
    {
        var secretKey = _jwt.SecretKey;
        var issuer = _jwt.Issuer;
        var audience = _jwt.Audience;
        var expirationDays = _jwt.ExpirationInDays;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, user.Role)
        };

        if (domainUserId is not null)
            claims.Add(new Claim("domainUserId", domainUserId.Value.ToString()));

        if (agentId is not null)
            claims.Add(new Claim("agentId", agentId.Value.ToString()));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
                  issuer: issuer,
                  audience: audience,
                  claims: claims,
                  expires: DateTime.UtcNow.AddDays(expirationDays),
                  signingCredentials: credentials
        ); 
        
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}