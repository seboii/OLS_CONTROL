using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OLS.Business.Common;
using OLS.DataAccess.Context;
using OLS.DataAccess.Entities;
using OLS.DataAccess.Siber;

namespace OLS.Business.Services.Authentication;

public interface IAuthService
{
    /// <summary>
    /// <paramref name="identifier"/> e-posta YA DA Siber kullanıcı kodu olabilir
    /// (bkz. <see cref="AuthService.LoginAsync"/>).
    /// </summary>
    Task<LoginResult> LoginAsync(string identifier, string password, CancellationToken cancellationToken = default);
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
    private readonly ISiberUserRepository _siberUsers;

    /// <summary>
    /// <c>Siber:LoginEnabled</c> (docker: <c>Siber__LoginEnabled</c>). Varsayılan
    /// AÇIK; kapatmak Siber şifresiyle girişi tamamen devre dışı bırakır ve
    /// yalnızca yerel şifreler kalır.
    /// </summary>
    private readonly bool _siberLoginEnabled;

    public AuthService(
        OlsDbContext db, IPasswordHasher passwordHasher, ITokenService tokenService,
        ISiberUserRepository siberUsers, IConfiguration configuration)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _siberUsers = siberUsers;
        _siberLoginEnabled = configuration.GetValue("Siber:LoginEnabled", true);
    }

    /// <summary>
    /// İKİ ŞİFRE KAYNAĞI: yerel bcrypt (<c>users.password</c>) ve Siber hesabı.
    ///
    /// Kullanıcılar zaten her gün Siber'e kendi kod+şifresiyle giriyor; ayrı bir
    /// şifreyi ezberlemek zorunda kalmasınlar diye Siber şifresi de kabul edilir.
    /// Şifre HİÇBİR ZAMAN buraya taşınmaz: doğrulama Siber sunucusunda
    /// <c>PWDCOMPARE</c> ile yapılır (bkz. <see cref="ISiberUserRepository"/>).
    ///
    /// Sıra bilinçli: önce yerel şifre denenir (Siber'e bağlanamasak da yerel
    /// giriş çalışmaya devam etsin), sonra Siber. Siber yolu YALNIZCA yerelde
    /// KAYITLI ve AKTİF bir kullanıcı için işler — Siber'de hesabı olan biri
    /// yerelde açılmadan içeri giremez, çünkü yetkiler yerel tabloda duruyor ve
    /// otomatik açılan bir hesap yetkisiz/denetimsiz olurdu.
    /// </summary>
    public async Task<LoginResult> LoginAsync(
        string identifier, string password, CancellationToken cancellationToken = default)
    {
        var user = await FindUserAsync(identifier, cancellationToken);

        // olsold sırası: önce pasiflik kontrolü, sonra şifre doğrulama.
        // Aynı sırayı koruyoruz ki kullanıcıya dönen mesaj değişmesin.
        if (user is not null && !user.Status)
            return LoginResult.Inactive();

        if (user is null || !await VerifyPasswordAsync(user, password, cancellationToken))
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
    /// Kullanıcıyı e-posta ya da Siber kullanıcı koduyla bulur.
    ///
    /// E-posta eşleşmesi kaynaktaki gibi TAM: <c>users.email</c> üzerinde
    /// benzersizlik var.
    ///
    /// SİBER KODU BELLEKTE EŞLEŞTİRİLİR — ve bu, bulunmuş GERÇEK bir hatanın
    /// düzeltmesidir. Önceki sürüm <c>u.SiberCode.ToLower() == trimmed.ToLower()</c>
    /// yazıyordu; buradaki <c>trimmed.ToLower()</c> yerel bir değişken olduğu için
    /// EF onu SQL'e çevirmiyor, .NET tarafında hesaplayıp parametre olarak
    /// gönderiyor. .NET'te <c>"İ".ToLower()</c> TEK harf üretmiyor — 'i' + birleşen
    /// nokta (U+0307), yani iki kod noktası. PostgreSQL'in <c>lower('İ')</c>'si ise
    /// tek 'i'. Sonuç: <c>FATİHT</c> kullanıcısı kodunu doğru yazdığında
    /// BULUNAMIYORDU (canlıda İ içeren 5 kod var). Aynı tuzağın ikinci yüzü de
    /// var: <c>lower('I')='i'</c> ama <c>lower('ı')='ı'</c>, yani 'ı' içeren
    /// kodlar (ör. <c>VıACHESLAVK</c>) SQL tarafında da eşleşmiyordu.
    ///
    /// İkisini birden çözen tek yer <see cref="QueryableExtensions.NormalizeTurkish"/>:
    /// İ/I/ı'nın üçünü de tek harfe indirger ve iki tarafa AYNI dönüşüm uygulanır.
    /// Kullanıcı tablosu 130 satır; giriş yolunda bunu belleğe çekmek ucuz.
    /// </summary>
    private async Task<User?> FindUserAsync(string identifier, CancellationToken cancellationToken)
    {
        var trimmed = identifier.Trim();

        var byEmail = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == trimmed && u.DeletedAt == null, cancellationToken);

        if (byEmail is not null)
            return byEmail;

        var wanted = QueryableExtensions.NormalizeTurkish(trimmed);

        var candidates = await _db.Users.AsNoTracking()
            .Where(u => u.DeletedAt == null && u.SiberCode != null && u.SiberCode != "")
            .Select(u => new { u.Id, u.SiberCode })
            .ToListAsync(cancellationToken);

        var match = candidates.FirstOrDefault(
            c => QueryableExtensions.NormalizeTurkish(c.SiberCode!.Trim()) == wanted);

        return match is null
            ? null
            : await _db.Users.FirstOrDefaultAsync(u => u.Id == match.Id, cancellationToken);
    }

    /// <summary>
    /// Önce yerel bcrypt, sonra Siber. Siber'e hiç gidilmeyen durumlar:
    /// özellik kapalı, bağlantı yok, kullanıcının Siber kodu yok ya da şifre boş.
    /// </summary>
    private async Task<bool> VerifyPasswordAsync(
        User user, string password, CancellationToken cancellationToken)
    {
        if (user.Password is not null && _passwordHasher.Verify(password, user.Password))
            return true;

        if (!_siberLoginEnabled || !_siberUsers.IsConfigured)
            return false;

        // Siber'de engelli hesap zaten senkronda yerelde pasife çekiliyor, ama
        // senkron turu arasında kalan bir engellemeyi de kaçırmayalım diye
        // depo tarafında ayrıca kontrol ediliyor.
        if (string.IsNullOrWhiteSpace(user.SiberCode) || string.IsNullOrEmpty(password))
            return false;

        var result = await _siberUsers.VerifyPasswordAsync(
            user.SiberCode, password, cancellationToken);

        return result == SiberPasswordResult.Success;
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
