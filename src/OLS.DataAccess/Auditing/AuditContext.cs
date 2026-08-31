namespace OLS.DataAccess.Auditing;

/// <summary>
/// Denetim kaydının kimin adına yazılacağını taşır.
///
/// DataAccess katmanı ICurrentUser'ı (Business katmanında) göremediği için
/// bu küçük arayüz burada tanımlanır; API katmanı HTTP bağlamından doldurur.
/// Arka plan servislerinde (Siber senkronu) doldurulmaz — <see cref="UserId"/>
/// null kalır ve interceptor hiçbir şey yazmaz. Kullanıcı eylemi ile senkronu
/// ayıran tek nokta budur (bkz. <see cref="Entities.AuditLog"/>).
/// </summary>
public interface IAuditContext
{
    long? UserId { get; }
    string? UserName { get; }
    string? IpAddress { get; }
}

/// <summary>Arka plan işleri için: denetim yazılmaz.</summary>
public sealed class NullAuditContext : IAuditContext
{
    public long? UserId => null;
    public string? UserName => null;
    public string? IpAddress => null;
}
