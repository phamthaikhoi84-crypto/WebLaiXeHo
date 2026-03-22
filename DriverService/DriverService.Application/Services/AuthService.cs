using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace DriverService.Application.Services;

public class AuthService
{
    private readonly string _jwtKey = "day_la_khoa_bao_mat_rat_dai_va_an_toan_nhat_co_the_12345";

    public string GenerateToken(string email, string role, Guid userId)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwtKey);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] {
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, role),
                new Claim("id", userId.ToString())
            }),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}