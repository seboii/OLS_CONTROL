using System.Net;
using Dapper;
using Microsoft.Extensions.Logging;

namespace OLS.DataAccess.Siber;

/// <summary>
/// Uygulamadan yüklenen dosyayı SİBER ARŞİVİNE yazar: önce FTP'ye kopyalar,
/// sonra <c>sbr_arsiv</c> kaydını açar.
///
/// MODÜL KODU — Siber'in klasör düzeninin ilk seviyesi. Canlı veriden çıkarıldı:
///   yük  → 0401 + iş türü (EX=0401, IM=0402, TR=0403, 3→0404); baskınlık %98-99
///   sefer → 0405 (2.400/2.400)
///   teklif → 04113 (3.685/3.685)
/// Bu kod hem klasör adı hem sbr_arsiv.modulkod olarak kullanılıyor; yanlış
/// verilirse dosya Siber'in kendi ekranında görünmez.
///
/// FTPAD — <c>sbr_arsiv.ftpad</c> IDENTITY sütunu, numarayı SQL Server üretiyor.
/// Kendi sayaç kurmuyoruz: MAX+1 deseni bu projede daha önce yarış durumu
/// üretmişti (rezervasyon/yük/sefer numaraları).
/// </summary>
public interface ISiberArchiveWriter
{
    bool IsConfigured { get; }

    /// <summary>
    /// Dosyayı arşive ekler. Başarılıysa oluşan arşiv kimliğini döner; Siber
    /// bağlantısı yoksa ya da yazılamazsa null.
    /// </summary>
    Task<string?> UploadAsync(
        SiberArchiveUpload upload, CancellationToken cancellationToken = default);
}

public sealed class SiberArchiveUpload
{
    /// <summary>Bağlanacağı Siber kaydının kimliği (yukid / pozisyonid / rezervasyonid).</summary>
    public string ModulId { get; init; } = string.Empty;

    /// <summary>Klasör düzeyi kodu — bkz. sınıf açıklaması.</summary>
    public string ModulKod { get; init; } = string.Empty;

    /// <summary>Kullanıcının gördüğü dosya adı (uzantısıyla).</summary>
    public string FileName { get; init; } = string.Empty;

    public byte[] Content { get; init; } = [];

    /// <summary>Kaydı açan kullanıcının Siber kodu.</summary>
    public string? UserCode { get; init; }
}

public sealed class SiberArchiveWriter : ISiberArchiveWriter
{
    private readonly ISiberConnectionFactory _factory;
    private readonly ILogger<SiberArchiveWriter> _logger;

    public SiberArchiveWriter(ISiberConnectionFactory factory, ILogger<SiberArchiveWriter> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public bool IsConfigured => _factory.IsConfigured;

    /// <summary>Yükün iş türünden modül kodu (bkz. sınıf açıklaması).</summary>
    public static string ModulKodForWorkType(int? isTuru) => isTuru switch
    {
        0 => "0401",
        1 => "0402",
        2 => "0403",
        3 => "0404",
        _ => "0403",
    };

    public const string ExpeditionModulKod = "0405";
    public const string ReservationModulKod = "04113";

    public async Task<string?> UploadAsync(
        SiberArchiveUpload upload, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured ||
            string.IsNullOrWhiteSpace(upload.ModulId) ||
            string.IsNullOrWhiteSpace(upload.ModulKod) ||
            string.IsNullOrWhiteSpace(upload.FileName) ||
            upload.Content.Length == 0 ||
            !Guid.TryParse(upload.ModulId, out var recordId))
        {
            return null;
        }

        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        var settings = await connection.QueryFirstOrDefaultAsync<(string? Ip, int? Port, string? User, string? Pass, string? Folder)>(
            new CommandDefinition("""
                SELECT TOP 1
                       LTRIM(RTRIM(arsivftpip))      AS Ip,
                       TRY_CAST(arsivftpport AS INT) AS Port,
                       LTRIM(RTRIM(arsivftpuser))    AS [User],
                       LTRIM(RTRIM(arsivftppass))    AS Pass,
                       LTRIM(RTRIM(arsivftpklasor))  AS Folder
                FROM sbr_parametre
                """, cancellationToken: cancellationToken));

        if (string.IsNullOrWhiteSpace(settings.Ip) || string.IsNullOrWhiteSpace(settings.User))
            return null;

        var arsivId = Guid.NewGuid();
        var folder = string.IsNullOrWhiteSpace(settings.Folder) ? "siberarsiv" : settings.Folder!;

        // ÖNCE veritabanı: ftpad IDENTITY olduğu için numarayı ancak INSERT
        // sonrasında öğrenebiliyoruz ve dosya adı ona bağlı. FTP adımı hata
        // verirse kayıt geri alınır — aksi hâlde Siber ekranında açılamayan bir
        // evrak satırı kalırdı.
        var ftpAd = await connection.ExecuteScalarAsync<long?>(new CommandDefinition("""
            INSERT INTO sbr_arsiv
                (arsivid, ad, modulkod, modulid, arsivftpklasor, kayitgiristarih, kayitgiren)
            VALUES
                (@ArsivId, @Ad, @ModulKod, @ModulId, @Klasor, @Tarih, @KayitGiren);
            SELECT CAST(SCOPE_IDENTITY() AS BIGINT);
            """,
            new
            {
                ArsivId = arsivId,
                Ad = upload.FileName,
                upload.ModulKod,
                ModulId = recordId,
                Klasor = folder,
                Tarih = DateTime.Now,
                KayitGiren = upload.UserCode,
            }, cancellationToken: cancellationToken));

        if (ftpAd is not { } fileNumber)
        {
            _logger.LogWarning("Siber arşiv kaydı açıldı ama ftpad okunamadı: {ArsivId}", arsivId);
            return null;
        }

        var basePath = $"ftp://{settings.Ip}:{settings.Port ?? 21}/{folder}/{upload.ModulKod}";
        var recordFolder = $"{basePath}/{{{recordId.ToString().ToUpperInvariant()}}}";

        try
        {
            // Kayda ait klasör ilk evrakta yoktur; oluşturulmazsa STOR 550 verir.
            EnsureDirectory(recordFolder, settings.User!, settings.Pass ?? string.Empty);

            await UploadFileAsync(
                $"{recordFolder}/{fileNumber}.SBR", settings.User!, settings.Pass ?? string.Empty,
                upload.Content, cancellationToken);

            return arsivId.ToString();
        }
        catch (WebException ex)
        {
            _logger.LogError(ex,
                "Siber arşivine dosya yüklenemedi ({Dosya}); arşiv kaydı geri alınıyor. Durum: {Status}",
                upload.FileName, ex.Status);

            await connection.ExecuteAsync(new CommandDefinition(
                "DELETE FROM sbr_arsiv WHERE arsivid = @ArsivId",
                new { ArsivId = arsivId }, cancellationToken: cancellationToken));

            return null;
        }
    }

    private static void EnsureDirectory(string uri, string user, string password)
    {
        try
        {
#pragma warning disable SYSLIB0014
            var request = (FtpWebRequest)WebRequest.Create(new Uri(uri));
#pragma warning restore SYSLIB0014
            request.Method = WebRequestMethods.Ftp.MakeDirectory;
            request.Credentials = new NetworkCredential(user, password);
            request.KeepAlive = false;
            request.Timeout = 30_000;

            using var response = (FtpWebResponse)request.GetResponse();
        }
        catch (WebException)
        {
            // Klasör zaten varsa sunucu 550 döner — bu bir hata değil, beklenen
            // durum. Gerçek erişim sorunu varsa bir sonraki STOR zaten patlar.
        }
    }

    private static async Task UploadFileAsync(
        string uri, string user, string password, byte[] content, CancellationToken cancellationToken)
    {
#pragma warning disable SYSLIB0014
        var request = (FtpWebRequest)WebRequest.Create(new Uri(uri));
#pragma warning restore SYSLIB0014
        request.Method = WebRequestMethods.Ftp.UploadFile;
        request.Credentials = new NetworkCredential(user, password);
        request.UseBinary = true;
        request.UsePassive = true;
        request.KeepAlive = false;
        request.Timeout = 30_000;
        request.ReadWriteTimeout = 120_000;
        request.ContentLength = content.Length;

        await using (var stream = await request.GetRequestStreamAsync())
            await stream.WriteAsync(content, cancellationToken);

        using var response = (FtpWebResponse)await request.GetResponseAsync();
    }
}
