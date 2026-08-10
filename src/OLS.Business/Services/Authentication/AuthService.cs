using Microsoft.EntityFrameworkCore;
using OLS.DataAccess.Context;
using OLS.DataAccess.Entities;

namespace OLS.Business.Services.Authentication;

public interface IAuthService
{
    Task<LoginResult> LoginAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<User?> GetAuthenticatedUserAsync(long userId, CancellationToken cancellationToken = default);
    Task RevokeAsync(string jti, long userId, DateTime expiresAt, CancellationToken cancellationToken = default);
    Task<bool> IsRevokedAsync(string jti, CancellationToken cancellationToken = default);
}

/// <summary>
/// Giriş sonucu. olsold'daki FrontLoginController::login üç ayrı yanıt üretiyordu:
/// pasif kullanıcı (401), hatalı kimlik (401), başarı (200).
/// </summary>
public sealed record LoginResult(LoginOutcome Outcome, User? User = null, string? Token = null)
{
    public static LoginResult Inactive() => new(LoginOutcome.Inactive);
    public static LoginResult Invalid() => new(LoginOutcome.InvalidCredentials);
    public static LoginResult Ok(User user, string token) => new(LoginOutcome.Success, user, token);
}

public enum LoginOutcome
{
    Success,
    InvalidCredentials,
    Inactive,
}

public sealed class AuthService : IAuthService
{
    private readonly OlsDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public AuthService(OlsDbContext db, IPasswordHasher passwordHasher, ITokenService tokenService)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<LoginResult> LoginAsync(
        string email, string password, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == email && u.DeletedAt == null, cancellationToken);

        // olsold sırası: önce pasiflik kontrolü, sonra şifre doğrulama.
        // Aynı sırayı koruyoruz ki kullanıcıya dönen mesaj değişmesin.
        if (user is not null && !user.Status)
            return LoginResult.Inactive();

        if (user?.Password is null || !_passwordHasher.Verify(password, user.Password))
            return LoginResult.Invalid();

        var (token, jti, expiresAt) = _tokenService.Create(user);

        _db.RevokedTokens.Add(new RevokedToken
        {
            Jti = jti,
            UserId = user.Id,
            ExpiresAt = expiresAt,
            RevokedAt = null,
        });
        await _db.SaveChangesAsync(cancellationToken);

        return LoginResult.Ok(user, token);
    }

    /// <summary>
    /// olsold'daki <c>Auth::user()->load('phoneCountryId', 'countryId')</c> karşılığı.
    /// Frontend bu iki ilişkiyi profil ekranında kullanıyor.
    /// </summary>
    public async Task<User?> GetAuthenticatedUserAsync(
        long userId, CancellationToken cancellationToken = default) =>
        await _db.Users
            .Include(u => u.Country)
            .Include(u => u.PhoneCountry)
            .FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null, cancellationToken);

    /// <summary>
    /// Passport'ta <c>token()->revoke()</c> vardı. JWT durumsuz olduğu için
    /// iptal edilen jetonun jti değerini saklıyoruz; doğrulama sırasında kontrol edilir.
    /// </summary>
    public async Task RevokeAsync(
        string jti, long userId, DateTime expiresAt, CancellationToken cancellationToken = default)
    {
        var existing = await _db.RevokedTokens
            .FirstOrDefaultAsync(t => t.Jti == jti, cancellationToken);

        if (existing is null)
        {
            _db.RevokedTokens.Add(new RevokedToken
            {
                Jti = jti,
                UserId = userId,
                ExpiresAt = expiresAt,
                RevokedAt = DateTime.UtcNow,
            });
        }
        else
        {
            existing.RevokedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> IsRevokedAsync(string jti, CancellationToken cancellationToken = default) =>
        await _db.RevokedTokens
            .AnyAsync(t => t.Jti == jti && t.RevokedAt != null, cancellationToken);
}
