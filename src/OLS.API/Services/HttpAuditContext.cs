using System.Security.Claims;
using OLS.DataAccess.Auditing;

namespace OLS.API.Services;

/// <summary>
/// Denetim bağlamını HTTP isteğinden doldurur.
///
/// Arka plan servisleri (Siber senkronu) kendi DI kapsamlarını açar ve orada
/// <c>HttpContext</c> NULL'dur — bu durumda <see cref="UserId"/> null döner ve
/// interceptor hiçbir denetim kaydı yazmaz. Kullanıcı eylemi / otomatik senkron
/// ayrımı tek bu noktada yapılır, ayrı bir bayrağa gerek kalmaz.
/// </summary>
public sealed class HttpAuditContext : IAuditContext
{
    private readonly IHttpContextAccessor _accessor;

    public HttpAuditContext(IHttpContextAccessor accessor) => _accessor = accessor;

    private ClaimsPrincipal? User => _accessor.HttpContext?.User;

    public long? UserId =>
        long.TryParse(User?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public string? UserName
    {
        get
        {
            var name = User?.FindFirstValue(ClaimTypes.Name);
            var email = User?.FindFirstValue(ClaimTypes.Email);

            return string.IsNullOrWhiteSpace(name) ? email : name;
        }
    }

    /// <summary>
    /// Ters vekil (nginx) arkasında çalıştığımız için gerçek istemci adresi
    /// X-Forwarded-For başlığında gelir; doğrudan RemoteIpAddress her zaman
    /// konteyner ağının adresini gösterirdi.
    /// </summary>
    public string? IpAddress
    {
        get
        {
            var context = _accessor.HttpContext;
            if (context is null)
                return null;

            var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(forwarded))
                return forwarded.Split(',')[0].Trim();

            return context.Connection.RemoteIpAddress?.ToString();
        }
    }
}
