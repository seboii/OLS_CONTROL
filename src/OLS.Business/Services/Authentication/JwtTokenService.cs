using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using OLS.DataAccess.Entities;

namespace OLS.Business.Services.Authentication;

public interface ITokenService
{
    /// <summary>Kullanıcı için erişim jetonu üretir. jti değeri iptal takibi için döner.</summary>
    (string Token, string Jti, DateTime ExpiresAt) Create(User user);
}

/// <summary>
/// Laravel Passport'un <c>$user->createToken('Token')->accessToken</c> karşılığı.
///
/// Passport OAuth2 access token üretiyordu; burada imzalı JWT kullanıyoruz.
/// Ömür, olsold'daki SESSION_LIFETIME=12000 dakika değerine denk gelecek
/// şekilde yapılandırılabilir (varsayılan aynı: 12000 dk).
/// </summary>
public sealed class JwtTokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration) => _configuration = configuration;

    public (string Token, string Jti, DateTime ExpiresAt) Create(User user)
    {
        var key = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key tanımlı değil.");

        var lifetimeMinutes = _configuration.GetValue<int?>("Jwt:LifetimeMinutes") ?? 12000;
        var expiresAt = DateTime.UtcNow.AddMinutes(lifetimeMinutes);
        var jti = Guid.NewGuid().ToString("N");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, jti),
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), jti, expiresAt);
    }
}
