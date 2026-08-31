using System.Net;
using Dapper;
using Microsoft.Extensions.Logging;

namespace OLS.DataAccess.Siber;

/// <summary>
/// Siber arşiv dosyalarını FTP'den çeker.
///
/// YOL DÜZENİ (canlıda çözüldü, tahmin değil):
///   ftp://{arsivftpip}/{arsivftpklasor}/{modulkod}/{MODULID}/{ftpad}.SBR
/// Örnek: siberarsiv/0403/{FE4DC557-D6B4-40D3-93C1-3CE8C72323B9}/428505.SBR
///
/// Üç ayrıntı önemli:
///   • MODULID süslü parantez içinde ve BÜYÜK harf yazılıyor.
///   • Dosya adı sbr_arsiv.ftpad + sabit ".SBR" uzantısı.
///   • ".SBR" bir sarmalayıcı DEĞİL — içerik dosyanın kendisi. Örnek dosyanın
///     ilk baytları "%PDF-1.6", yani doğrudan servis edilebiliyor. Gerçek tür,
///     kullanıcıya gösterilen ad (sbr_arsiv.ad) uzantısından belirlenir.
///
/// Bağlantı bilgileri sbr_parametre'den okunur; Siber zaten tek doğruluk noktası
/// ve parolayı .env'e kopyalamak ikinci bir sır yönetimi yükü olurdu. Ayarlar
/// süreç boyunca önbelleklenir (tek satırlık, değişmeyen yapılandırma).
/// </summary>
public interface ISiberArchiveFileReader
{
    /// <summary>Dosyayı indirir; bulunamazsa null döner.</summary>
    Task<byte[]?> DownloadAsync(
        string modulKod, string modulId, string ftpAd, CancellationToken cancellationToken = default);
}

public sealed class SiberArchiveFileReader : ISiberArchiveFileReader
{
    private readonly ISiberConnectionFactory _factory;
    private readonly ILogger<SiberArchiveFileReader> _logger;
    private ArchiveFtpSettings? _cached;

    public SiberArchiveFileReader(
        ISiberConnectionFactory factory, ILogger<SiberArchiveFileReader> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    private sealed record ArchiveFtpSettings(string Host, int Port, string User, string Password);

    private async Task<ArchiveFtpSettings?> SettingsAsync(CancellationToken cancellationToken)
    {
        if (_cached is not null)
            return _cached;

        if (!_factory.IsConfigured)
            return null;

        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        var row = await connection.QueryFirstOrDefaultAsync<(string? Ip, int? Port, string? User, string? Pass)>(
            new CommandDefinition("""
                SELECT TOP 1
                       LTRIM(RTRIM(arsivftpip))   AS Ip,
                       TRY_CAST(arsivftpport AS INT) AS Port,
                       LTRIM(RTRIM(arsivftpuser)) AS [User],
                       LTRIM(RTRIM(arsivftppass)) AS Pass
                FROM sbr_parametre
                """, cancellationToken: cancellationToken));

        if (string.IsNullOrWhiteSpace(row.Ip) || string.IsNullOrWhiteSpace(row.User))
            return null;

        return _cached = new ArchiveFtpSettings(row.Ip!, row.Port ?? 21, row.User!, row.Pass ?? string.Empty);
    }

    public async Task<byte[]?> DownloadAsync(
        string modulKod, string modulId, string ftpAd, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(modulKod) ||
            string.IsNullOrWhiteSpace(modulId) ||
            string.IsNullOrWhiteSpace(ftpAd))
        {
            return null;
        }

        var settings = await SettingsAsync(cancellationToken);
        if (settings is null)
            return null;

        // Yol parçaları yalnızca Siber'den gelen kimliklerden kuruluyor; yine de
        // dizin atlama (../) denemelerine kapalı olsun diye kaba doğrulama.
        if (modulKod.Contains('/') || modulKod.Contains('\\') || modulKod.Contains("..") ||
            ftpAd.Contains('/') || ftpAd.Contains('\\') || ftpAd.Contains("..") ||
            !Guid.TryParse(modulId, out var recordId))
        {
            return null;
        }

        var folder = _cachedFolder ??= await FolderAsync(cancellationToken) ?? "siberarsiv";

        var uri = new Uri(
            $"ftp://{settings.Host}:{settings.Port}/{folder}/{modulKod}/" +
            $"{{{recordId.ToString().ToUpperInvariant()}}}/{ftpAd}.SBR");

#pragma warning disable SYSLIB0014 // .NET'te yerleşik modern FTP istemcisi yok; ek paket almamak için bilinçli.
        var request = (FtpWebRequest)WebRequest.Create(uri);
#pragma warning restore SYSLIB0014
        request.Method = WebRequestMethods.Ftp.DownloadFile;
        request.Credentials = new NetworkCredential(settings.User, settings.Password);
        request.UseBinary = true;
        request.UsePassive = true;
        request.KeepAlive = false;
        request.Timeout = 30_000;
        request.ReadWriteTimeout = 60_000;

        try
        {
            using var response = (FtpWebResponse)await request.GetResponseAsync();
            await using var stream = response.GetResponseStream();
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, cancellationToken);
            return buffer.ToArray();
        }
        catch (WebException ex)
        {
            // SESSİZ YUTMA YOK: ilk sürümde istisna yutulduğu için "evrak
            // açılamadı" hatasının sebebi loglarda hiç görünmüyordu ve teşhis
            // için sunucuyu elle kurcalamak gerekti. Artık neden ve yol yazılır.
            _logger.LogWarning(ex,
                "Siber arşiv dosyası indirilemedi. Yol: {Uri} — durum: {Status}",
                uri, ex.Status);
            return null;
        }
    }

    private string? _cachedFolder;

    private async Task<string?> FolderAsync(CancellationToken cancellationToken)
    {
        if (!_factory.IsConfigured)
            return null;

        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<string?>(
            new CommandDefinition(
                "SELECT TOP 1 LTRIM(RTRIM(arsivftpklasor)) FROM sbr_parametre",
                cancellationToken: cancellationToken));
    }
}
