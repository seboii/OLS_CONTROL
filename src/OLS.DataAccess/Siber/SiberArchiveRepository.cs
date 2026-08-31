using Dapper;

namespace OLS.DataAccess.Siber;

/// <summary>
/// Siber'in evrak arşivi (<c>sbr_arsiv</c>).
///
/// Siber taranmış evrakları VERİTABANINDA tutmuyor: <c>skn_yukevrak</c> yalnızca
/// bir takip kaydıdır (evrak türü, numarası, kaç orijinal/kopya, kim teslim
/// aldı) ve dosya sütunu yoktur. Dosyaların kendisi bir FTP arşiv sunucusunda
/// duruyor; <c>sbr_parametre</c> içindeki <c>arsivftpip/klasor/user/pass</c>
/// ayarlarıyla yapılandırılmış. <c>sbr_arsiv</c> ikisini birleştiren indekstir:
/// <c>ad</c> kullanıcının gördüğü dosya adı, <c>ftpad</c> FTP'deki gerçek ad.
///
/// Kayda bağlanma <c>modulid</c> üzerinden ve doğrudan bizim zaten tuttuğumuz
/// Siber kimlikleriyle eşleşiyor (canlıda doğrulandı): yükte skn_yuk.yukid ile
/// 24.863, seferde skn_pozisyon.pozisyonid ile 11.195, teklifte
/// skn_rezervasyon.rezervasyonid ile 3.685 eşleşme.
/// </summary>
public interface ISiberArchiveRepository
{
    bool IsConfigured { get; }

    Task<IReadOnlyList<SiberArsivKaydi>> ListByModuleAsync(
        string modulId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Birden çok kaydın arşivini TEK sorguda getirir. Sefer ekranında bağlı
    /// yüklerin evrakları da gösteriliyor; yük başına ayrı sorgu atmak seferdeki
    /// yük sayısı kadar gidiş-dönüş demekti.
    /// </summary>
    Task<IReadOnlyDictionary<string, IReadOnlyList<SiberArsivKaydi>>> ListByModulesAsync(
        IReadOnlyCollection<string> modulIds, CancellationToken cancellationToken = default);

    /// <summary>Tek arşiv kaydı — indirme ucunun dosya yolunu kurabilmesi için.</summary>
    Task<SiberArsivKaydi?> FindAsync(string arsivId, CancellationToken cancellationToken = default);
}

public sealed class SiberArsivKaydi
{
    public string ArsivId { get; init; } = string.Empty;

    /// <summary>Kullanıcının gördüğü dosya adı (ör. "MAWB 555-16712684.pdf").</summary>
    public string? Ad { get; init; }

    /// <summary>FTP'deki gerçek dosya adı — genelde sayısal.</summary>
    public string? FtpAd { get; init; }

    public string? Klasor { get; init; }

    /// <summary>FTP klasör düzeninin ilk seviyesi (ör. "0403").</summary>
    public string? ModulKod { get; init; }

    /// <summary>Bağlı olduğu kaydın Siber kimliği — FTP yolunda klasör adı.</summary>
    public string? ModulId { get; init; }

    public string? Aciklama { get; init; }

    public DateTime? KayitGirisTarih { get; init; }

    public string? KayitGiren { get; init; }

    /// <summary>Siber'de müşteri portalına kapatılmış mı.</summary>
    public bool MusteriyeKapali { get; init; }

    /// <summary>KVKK işareti — arayüzde uyarı olarak gösterilir.</summary>
    public bool KisiselVeri { get; init; }

    /// <summary>Doluysa yalnızca bu gruplar görebilir (Siber'in kendi grup adları).</summary>
    public string? YetkiliGruplar { get; init; }
}

public sealed class SiberArchiveRepository : ISiberArchiveRepository
{
    private readonly ISiberConnectionFactory _factory;

    public SiberArchiveRepository(ISiberConnectionFactory factory) => _factory = factory;

    public bool IsConfigured => _factory.IsConfigured;

    public async Task<IReadOnlyList<SiberArsivKaydi>> ListByModuleAsync(
        string modulId, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured || string.IsNullOrWhiteSpace(modulId))
            return [];

        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        // uniqueidentifier sütunları string'e okunacaksa CAST şart — Dapper aksi
        // hâlde "Object must implement IConvertible" atıyor (proje kuralı).
        const string sql = """
            SELECT CAST(arsivid AS VARCHAR(64)) AS ArsivId,
                   ad AS Ad,
                   LTRIM(RTRIM(ftpad)) AS FtpAd,
                   LTRIM(RTRIM(arsivftpklasor)) AS Klasor,
                   LTRIM(RTRIM(modulkod)) AS ModulKod,
                   CAST(modulid AS VARCHAR(64)) AS ModulId,
                   aciklama AS Aciklama,
                   kayitgiristarih AS KayitGirisTarih,
                   LTRIM(RTRIM(kayitgiren)) AS KayitGiren,
                   CAST(ISNULL(musteridegorunmesin, 0) AS BIT) AS MusteriyeKapali,
                   CAST(ISNULL(kisiselveri, 0) AS BIT) AS KisiselVeri,
                   LTRIM(RTRIM(ISNULL(yetkiligruplar, ''))) AS YetkiliGruplar
            FROM sbr_arsiv
            WHERE CAST(modulid AS VARCHAR(64)) = @modulId
              AND ISNULL(pasif, 0) = 0
            ORDER BY kayitgiristarih DESC
            """;

        var rows = await connection.QueryAsync<SiberArsivKaydi>(
            new CommandDefinition(sql, new { modulId }, cancellationToken: cancellationToken));

        return rows.ToList();
    }

    public async Task<IReadOnlyDictionary<string, IReadOnlyList<SiberArsivKaydi>>> ListByModulesAsync(
        IReadOnlyCollection<string> modulIds, CancellationToken cancellationToken = default)
    {
        var ids = modulIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!IsConfigured || ids.Count == 0)
            return new Dictionary<string, IReadOnlyList<SiberArsivKaydi>>();

        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        const string sql = """
            SELECT CAST(arsivid AS VARCHAR(64)) AS ArsivId,
                   ad AS Ad,
                   LTRIM(RTRIM(ftpad)) AS FtpAd,
                   LTRIM(RTRIM(arsivftpklasor)) AS Klasor,
                   LTRIM(RTRIM(modulkod)) AS ModulKod,
                   CAST(modulid AS VARCHAR(64)) AS ModulId,
                   aciklama AS Aciklama,
                   kayitgiristarih AS KayitGirisTarih,
                   LTRIM(RTRIM(kayitgiren)) AS KayitGiren,
                   CAST(ISNULL(musteridegorunmesin, 0) AS BIT) AS MusteriyeKapali,
                   CAST(ISNULL(kisiselveri, 0) AS BIT) AS KisiselVeri,
                   LTRIM(RTRIM(ISNULL(yetkiligruplar, ''))) AS YetkiliGruplar
            FROM sbr_arsiv
            WHERE CAST(modulid AS VARCHAR(64)) IN @ids AND ISNULL(pasif, 0) = 0
            ORDER BY kayitgiristarih DESC
            """;

        var rows = await connection.QueryAsync<SiberArsivKaydi>(
            new CommandDefinition(sql, new { ids }, cancellationToken: cancellationToken));

        return rows
            .Where(r => r.ModulId is not null)
            .GroupBy(r => r.ModulId!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<SiberArsivKaydi>)g.ToList(),
                StringComparer.OrdinalIgnoreCase);
    }

    public async Task<SiberArsivKaydi?> FindAsync(
        string arsivId, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured || string.IsNullOrWhiteSpace(arsivId))
            return null;

        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        const string sql = """
            SELECT CAST(arsivid AS VARCHAR(64)) AS ArsivId,
                   ad AS Ad,
                   LTRIM(RTRIM(ftpad)) AS FtpAd,
                   LTRIM(RTRIM(arsivftpklasor)) AS Klasor,
                   LTRIM(RTRIM(modulkod)) AS ModulKod,
                   CAST(modulid AS VARCHAR(64)) AS ModulId,
                   aciklama AS Aciklama,
                   kayitgiristarih AS KayitGirisTarih,
                   LTRIM(RTRIM(kayitgiren)) AS KayitGiren,
                   CAST(ISNULL(musteridegorunmesin, 0) AS BIT) AS MusteriyeKapali,
                   CAST(ISNULL(kisiselveri, 0) AS BIT) AS KisiselVeri,
                   LTRIM(RTRIM(ISNULL(yetkiligruplar, ''))) AS YetkiliGruplar
            FROM sbr_arsiv
            WHERE CAST(arsivid AS VARCHAR(64)) = @arsivId AND ISNULL(pasif, 0) = 0
            """;

        return await connection.QueryFirstOrDefaultAsync<SiberArsivKaydi>(
            new CommandDefinition(sql, new { arsivId }, cancellationToken: cancellationToken));
    }
}
