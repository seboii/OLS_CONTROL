using Microsoft.EntityFrameworkCore;
using OLS.DataAccess.Context;

namespace OLS.Business.Services.Authorization;

/// <summary>
/// Şirket görünürlüğü — AVRORA / OLS ayrımı.
///
/// Siber'de iki şirket var (sbr_sirket) ve her yük/sefer birine ait:
/// AVRORA 760 yük / 276 sefer, OLS 7.210 yük / 4.100 sefer.
///
/// KURAL (kullanıcı isteği):
///   • Avrora ekibi (e-postası avroralog.com olan ya da elle Avrora kapsamına
///     alınan kullanıcı) YALNIZCA Avrora kayıtlarını görür.
///   • Süper admin / Yönetim her şeyi görür.
///   • Diğer herkes Avrora kayıtlarını GÖRMEZ; kalan her şeyi görür.
///
/// Neden rol değil ayrı bir alan: rol "ne yapabilir" (yetki şablonu), kapsam
/// "ne görebilir" (veri filtresi). Rolle birleştirilseydi her rolün şirket
/// başına kopyası gerekirdi (Satış-Avrora, Satış-OLS, Operasyon-Avrora…).
/// </summary>
public interface ICompanyScope
{
    /// <summary>
    /// Kullanıcının görebileceği kayıtlara filtre uygular. Sorgu kaynağı
    /// yük/sefer olabildiği için şirket kimliğini seçen bir ifade alır.
    /// </summary>
    Task<CompanyVisibility> ResolveAsync(long? userId, CancellationToken cancellationToken = default);
}

/// <summary>Bir kullanıcının görünürlük kararı.</summary>
public sealed record CompanyVisibility(bool SeesEverything, string? OnlyCompanyId, string? ExcludeCompanyId)
{
    /// <summary>Verilen kaydın şirketi bu kullanıcıya görünür mü.</summary>
    public bool Allows(string? companyId)
    {
        if (SeesEverything)
            return true;

        if (OnlyCompanyId is not null)
            return string.Equals(companyId, OnlyCompanyId, StringComparison.OrdinalIgnoreCase);

        return ExcludeCompanyId is null ||
               !string.Equals(companyId, ExcludeCompanyId, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class CompanyScope : ICompanyScope
{
    /// <summary>sbr_sirket: AVRORA ULUSLARARASI TASIMACILIK LIMITED SIRKETI.</summary>
    public const string AvroraCompanyId = "46258A01-8D77-4F87-AAF5-6B331DEDD8A7";

    /// <summary>Avrora ekibinin e-posta alan adı — otomatik kapsam ataması için.</summary>
    public const string AvroraEmailDomain = "@avroralog.com";

    private readonly OlsDbContext _db;
    private readonly IPermissionService _permissions;

    public CompanyScope(OlsDbContext db, IPermissionService permissions)
    {
        _db = db;
        _permissions = permissions;
    }

    public async Task<CompanyVisibility> ResolveAsync(
        long? userId, CancellationToken cancellationToken = default)
    {
        // Oturum yoksa (arka plan işi) filtre uygulanmaz — senkron tüm kayıtları
        // yazmak zorunda.
        if (userId is not { } id)
            return new CompanyVisibility(true, null, null);

        var isSuperAdmin = await _permissions.HasPermissionAsync(
            id, "super_admin", PermissionAction.Read, cancellationToken);

        if (isSuperAdmin)
            return new CompanyVisibility(true, null, null);

        var user = await _db.Users.AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new { u.SiberCompanyId, u.Email })
            .FirstOrDefaultAsync(cancellationToken);

        // Elle atanmış kapsam önceliklidir; yoksa e-posta alan adına bakılır.
        // Böylece Avrora ekibine sonradan katılan biri, e-postası farklı olsa
        // bile Kullanıcılar ekranından kapsama alınabilir.
        var scoped = user?.SiberCompanyId;

        if (string.IsNullOrWhiteSpace(scoped) &&
            user?.Email?.EndsWith(AvroraEmailDomain, StringComparison.OrdinalIgnoreCase) == true)
        {
            scoped = AvroraCompanyId;
        }

        return string.IsNullOrWhiteSpace(scoped)
            ? new CompanyVisibility(false, null, AvroraCompanyId)
            : new CompanyVisibility(false, scoped, null);
    }
}
