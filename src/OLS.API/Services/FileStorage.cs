using System.Security.Cryptography;

namespace OLS.API.Services;

public interface IFileStorage
{
    /// <summary>
    /// Avatar/dosya kaydeder ve üretilen dosya adını döner. Dosya yoksa null.
    /// </summary>
    Task<string?> SaveAvatarAsync(IFormFile? file, CancellationToken cancellationToken = default);

    /// <summary>Kayıtlı dosyayı siler. Yoksa sessizce geçer.</summary>
    void Delete(string? fileName);

    /// <summary>
    /// Yük ekleri gibi belge dosyalarını kaydeder. Görsellere ek olarak
    /// PDF/Office/arşiv türlerine de izin verir.
    /// </summary>
    Task<string?> SaveDocumentAsync(IFormFile? file, CancellationToken cancellationToken = default);

    /// <summary>
    /// Belgeyi bir alt klasöre yazar ve <b>yalnızca dosya adını</b> döner
    /// (klasör adı dönmez — vekaletname arayüzü yolu kendisi ekliyor).
    /// </summary>
    Task<string?> SaveDocumentAsync(
        IFormFile? file, string subDirectory, CancellationToken cancellationToken = default);

    /// <summary>Alt klasördeki dosyayı siler.</summary>
    void Delete(string? fileName, string subDirectory);
}

/// <summary>
/// olsold dosyaları <c>public/storage/</c> altına şu adla yazıyordu:
/// <c>uniqid() + bin2hex(random_bytes(6)) + md5(orijinalAd) + time() + .uzanti</c>
///
/// Aynı biçimi koruyoruz ki olimpikgama'dan taşınan 2.680 mevcut dosyanın
/// adlandırma düzeniyle tutarlı kalsın.
/// </summary>
public sealed class FileStorage : IFileStorage
{
    /// <summary>olsold'un kabul ettiği görsel türleri.</summary>
    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".gif", ".webp", ".svg" };

    private const long MaxBytes = 10 * 1024 * 1024;

    private readonly string _root;
    private readonly ILogger<FileStorage> _logger;

    public FileStorage(IConfiguration configuration, ILogger<FileStorage> logger)
    {
        _logger = logger;
        _root = configuration["Storage:PublicPath"] ?? "/app/storage/app/public";
        Directory.CreateDirectory(_root);
    }

    /// <summary>Yük ekleri için izinli belge türleri (olsold'da doğrulama yoktu).</summary>
    private static readonly HashSet<string> AllowedDocumentExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".csv", ".txt",
            ".eml", ".msg", ".zip", ".rar", ".7z",
        };

    private const long MaxDocumentBytes = 50 * 1024 * 1024;

    public Task<string?> SaveAvatarAsync(
        IFormFile? file, CancellationToken cancellationToken = default) =>
        SaveAsync(file, AllowedExtensions, MaxBytes, cancellationToken);

    public Task<string?> SaveDocumentAsync(
        IFormFile? file, CancellationToken cancellationToken = default)
    {
        // Belgeler görselleri de kapsar (ekran görüntüsü, taranmış evrak vb.)
        var allowed = new HashSet<string>(AllowedDocumentExtensions, StringComparer.OrdinalIgnoreCase);
        allowed.UnionWith(AllowedExtensions);

        return SaveAsync(file, allowed, MaxDocumentBytes, cancellationToken);
    }

    public Task<string?> SaveDocumentAsync(
        IFormFile? file, string subDirectory, CancellationToken cancellationToken = default)
    {
        var allowed = new HashSet<string>(AllowedDocumentExtensions, StringComparer.OrdinalIgnoreCase);
        allowed.UnionWith(AllowedExtensions);

        return SaveAsync(file, allowed, MaxDocumentBytes, cancellationToken, subDirectory);
    }

    private async Task<string?> SaveAsync(
        IFormFile? file, IReadOnlySet<string> allowedExtensions, long maxBytes,
        CancellationToken cancellationToken, string? subDirectory = null)
    {
        if (file is null || file.Length == 0)
            return null;

        if (file.Length > maxBytes)
            throw new InvalidOperationException(
                $"Dosya boyutu sınırı aşıldı ({maxBytes / 1024 / 1024} MB).");

        var extension = Path.GetExtension(file.FileName);

        // olsold uzantı doğrulaması yapmıyordu; yüklenen her şeyi public dizine
        // yazıyordu. Beyaz liste ekledik.
        if (!allowedExtensions.Contains(extension))
            throw new InvalidOperationException($"İzin verilmeyen dosya türü: {extension}");

        var fileName = BuildFileName(file.FileName, extension);
        var directory = ResolveDirectory(subDirectory);
        var path = Path.Combine(directory, fileName);

        await using var stream = new FileStream(path, FileMode.CreateNew);
        await file.CopyToAsync(stream, cancellationToken);

        // Alt klasöre yazılsa bile yalnızca dosya adı döner; klasör bilgisi
        // çağıran tarafın sabiti (arayüz URL'i kendisi kuruyor).
        return fileName;
    }

    public void Delete(string? fileName) => DeleteFrom(fileName, null);

    public void Delete(string? fileName, string subDirectory) =>
        DeleteFrom(fileName, subDirectory);

    private void DeleteFrom(string? fileName, string? subDirectory)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return;

        // Yol gezinmesine karşı: yalnızca dosya adı kısmını kullan.
        var safeName = Path.GetFileName(fileName);
        var path = Path.Combine(ResolveDirectory(subDirectory), safeName);

        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Dosya silinemedi: {Path}", path);
        }
    }

    /// <summary>Alt klasörü çözer ve yoksa oluşturur; yol gezinmesini engeller.</summary>
    private string ResolveDirectory(string? subDirectory)
    {
        if (string.IsNullOrWhiteSpace(subDirectory))
            return _root;

        var safe = Path.GetFileName(subDirectory);
        var directory = Path.Combine(_root, safe);
        Directory.CreateDirectory(directory);

        return directory;
    }

    private static string BuildFileName(string originalName, string extension)
    {
        // uniqid() karşılığı: mikrosaniye çözünürlüklü onaltılık zaman damgası
        var uniqueId = DateTime.UtcNow.Ticks.ToString("x13");
        var random = Convert.ToHexString(RandomNumberGenerator.GetBytes(6)).ToLowerInvariant();
        var nameHash = Convert.ToHexString(
            MD5.HashData(System.Text.Encoding.UTF8.GetBytes(originalName))).ToLowerInvariant();
        var unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        return $"{uniqueId}{random}{nameHash}{unixTime}{extension}";
    }
}
