using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SinochkuGames.Server.Data;

namespace SinochkuGames.Server.Services;

public sealed class JwtTokenService(IConfiguration configuration)
{
    public string Create(AppUser user)
    {
        var key = configuration["SINOCHKU_JWT_KEY"]
                  ?? throw new InvalidOperationException("SINOCHKU_JWT_KEY is not configured.");

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.UserName ?? ""),
            new Claim("founder", user.IsFounder ? "true" : "false")
        };

        var token = new JwtSecurityToken(
            issuer: "SinochkuGames",
            audience: "SinochkuGamesDesktop",
            claims: claims,
            expires: DateTime.UtcNow.AddDays(30),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
