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
///
/// Şirket ayrımı yalnızca GÖRÜNÜRLÜK değil: iki şirketin yük açma yolu da
/// farklı — bkz. <see cref="CompanyCapabilities"/>.
/// </summary>
public interface ICompanyScope
{
    /// <summary>
    /// Kullanıcının görebileceği kayıtlara filtre uygular. Sorgu kaynağı
    /// yük/sefer olabildiği için şirket kimliğini seçen bir ifade alır.
    /// </summary>
    Task<CompanyVisibility> ResolveAsync(long? userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kullanıcının şirketi hangi modülleri kullanıyor. Görünürlükten (hangi
    /// kaydı görür) AYRI bir soru: burada "bu iş akışı bu şirkette var mı"
    /// sorulur.
    /// </summary>
    Task<CompanyCapabilities> ResolveCapabilitiesAsync(
        long? userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// YENİ KAYIT HANGİ ŞİRKETE YAZILACAK.
    ///
    /// Tek kaynak: yük, sefer, araç ve cari açma akışlarının hepsi buradan
    /// geçer. Kural sırayla:
    ///
    ///   1. Kullanıcı tek şirkete bağlıysa (Avrora ekibi) DAİMA o şirket —
    ///      istenen değer yok sayılır. Aksi hâlde kullanıcı, kendi
    ///      listesinde göremeyeceği bir kayıt açabilirdi.
    ///   2. İki şirketi de gören kullanıcı (süper admin) <paramref name="requested"/>
    ///      ile seçebilir; tanımsız/bilinmeyen değer OLS'e düşer.
    ///   3. Diğer herkes OLS.
    /// </summary>
    Task<string> ResolveWriteCompanyAsync(
        long? userId, string? requested, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kullanıcının seçebileceği şirketler. Tek şirkete bağlı kullanıcıda tek
    /// öge döner; arayüz bir taneyse seçici göstermez.
    /// </summary>
    Task<IReadOnlyList<CompanyOption>> ListWritableCompaniesAsync(
        long? userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kullanıcı kaydın şirketini SEÇEBİLİR mi (yeni kayıtta ve mevcut kaydı
    /// taşırken). Yalnızca iki şirketi de gören kullanıcı için true.
    /// </summary>
    Task<bool> CanChooseCompanyAsync(long? userId, CancellationToken cancellationToken = default);
}

/// <summary>Şirket seçicisinin bir seçeneği.</summary>
public sealed record CompanyOption(string Id, string Name);

/// <summary>
/// Şirkete göre AÇIK/KAPALI iş akışları.
///
/// OLS ve Avrora bu noktada iki ayrı şirket gibi çalışıyor ve yük açma
/// yolları BİRBİRİNİ DIŞLIYOR:
///
///   • Avrora teklif kullanmıyor. Teklif sekmesi hiç görünmez; yük doğrudan
///     Yükler ekranından açılır.
///   • OLS teklifle çalışır. Her yük bir teklifin dönüşümüdür, bu yüzden
///     teklifsiz yük açma düğmesi yoktur.
///
/// Süper admin iki şirketi de yönettiği için her ikisine de erişir.
/// </summary>
public sealed record CompanyCapabilities(bool UsesOffers, bool CanCreateDirectLoad);

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

    /// <summary>sbr_sirket: OLS LOJISTIK — varsayılan şirket.</summary>
    public const string OlsCompanyId = "BA4888B1-A2B0-4142-B273-92481D932EAD";

    /// <summary>
    /// Seçilebilir şirketler. <c>sbr_sirket</c> tablosunda TAM OLARAK iki satır
    /// var; liste oradan çekilmiyor çünkü kimlikler zaten kod genelinde sabit
    /// (şube eşlemesi, yetenek kuralı, görünürlük filtresi hepsi bu ikisine
    /// dayanıyor) ve Siber'e bağlanamadığında da seçicinin çalışması gerekiyor.
    /// </summary>
    public static readonly IReadOnlyList<CompanyOption> Companies =
    [
        new(OlsCompanyId, "OLS"),
        new(AvroraCompanyId, "AVRORA"),
    ];

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

    public async Task<CompanyCapabilities> ResolveCapabilitiesAsync(
        long? userId, CancellationToken cancellationToken = default)
    {
        var visibility = await ResolveAsync(userId, cancellationToken);

        // Süper admin (ve oturumsuz arka plan işi) iki şirketi de yönetir.
        if (visibility.SeesEverything)
            return new CompanyCapabilities(UsesOffers: true, CanCreateDirectLoad: true);

        var isAvrora = string.Equals(visibility.OnlyCompanyId, AvroraCompanyId,
            StringComparison.OrdinalIgnoreCase);

        // Tam olarak birbirinin tersi: Avrora teklifsiz açar, OLS teklifle.
        return new CompanyCapabilities(UsesOffers: !isAvrora, CanCreateDirectLoad: isAvrora);
    }

    public async Task<string> ResolveWriteCompanyAsync(
        long? userId, string? requested, CancellationToken cancellationToken = default)
    {
        var visibility = await ResolveAsync(userId, cancellationToken);

        // Kapsamı olan kullanıcı seçemez: göremeyeceği bir kayıt açmasın.
        if (visibility.OnlyCompanyId is { } scoped)
            return scoped;

        if (!visibility.SeesEverything)
            return OlsCompanyId;

        return Companies.Any(c => string.Equals(c.Id, requested, StringComparison.OrdinalIgnoreCase))
            ? requested!
            : OlsCompanyId;
    }

    public async Task<bool> CanChooseCompanyAsync(
        long? userId, CancellationToken cancellationToken = default) =>
        (await ResolveAsync(userId, cancellationToken)).SeesEverything;

    public async Task<IReadOnlyList<CompanyOption>> ListWritableCompaniesAsync(
        long? userId, CancellationToken cancellationToken = default)
    {
        var visibility = await ResolveAsync(userId, cancellationToken);

        if (visibility.SeesEverything)
            return Companies;

        var only = visibility.OnlyCompanyId ?? OlsCompanyId;
        return Companies.Where(c => string.Equals(c.Id, only, StringComparison.OrdinalIgnoreCase)).ToList();
    }
}
