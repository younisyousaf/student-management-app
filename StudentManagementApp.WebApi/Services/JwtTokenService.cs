using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using StudentManagement.Infrastructure.Hybrid.Identity;
using StudentManagementApp.WebApi.Security;

namespace StudentManagementApp.WebApi.Services;

public sealed class JwtTokenService(IConfiguration configuration) : IJwtTokenService
{
    public string Generate(
        ApplicationUser user,
        IEnumerable<string> roles,
        int? schoolId = null)
    {
        var settings = configuration.GetSection("JwtSettings");

        var secret = settings["SecretKey"]
            ?? throw new InvalidOperationException("JWT Secret Key is missing.");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty)
        };

        claims.AddRange(
            roles.Select(role =>
                new Claim(ClaimTypes.Role, role)));

        if (schoolId.HasValue)
        {
            claims.Add(
                new Claim(
                    SmartCampusClaimTypes.SchoolId,
                    schoolId.Value.ToString()));
        }

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(secret)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: settings["Issuer"],
            audience: settings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(
                double.Parse(
                    settings["DurationInMinutes"] ?? "180")),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }
}