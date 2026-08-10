using Microsoft.EntityFrameworkCore;
using OLS.Business.Common;
using OLS.DataAccess.Context;
using OLS.DataAccess.Entities;

namespace OLS.Business.Services.Loads;

/// <summary>
/// Yük eklerinin toplu senkronu — <c>POST /load/file/upload</c>.
/// olsold: <c>LoadController::fileUpload</c>
///
/// Uç bir "diff" uygular: istekte gelen <c>files[]</c> dizisinin
/// <list type="bullet">
/// <item><c>id</c> taşıyan elemanları = KORUNACAK mevcut dosyalar</item>
/// <item><c>file</c> taşıyan elemanları = YENİ yüklenecek dosyalar</item>
/// </list>
/// Yükün diğer tüm dosyaları (listede id'si geçmeyenler) diskten ve
/// veritabanından SİLİNİR. Yani istek, yükün dosya listesinin son hâlidir.
///
/// Dosyaların diske yazımı API katmanında (<c>IFileStorage</c>) yapılır;
/// buraya yalnızca kaydedilmiş adlar gelir.
/// </summary>
public interface ILoadFileService
{
    /// <summary>
    /// Silinen dosyaların adlarını döner — çağıran bunları diskten temizler.
    /// </summary>
    Task<IReadOnlyList<string>> SyncAsync(
        long loadId, IReadOnlyList<long> keepIds, IReadOnlyList<NewLoadFile> newFiles,
        CancellationToken cancellationToken = default);
}

public sealed record NewLoadFile(string StoredName, string? Extension, string? OriginalName);

public sealed class LoadFileService : ILoadFileService
{
    private readonly OlsDbContext _db;
    private readonly IClock _clock;

    public LoadFileService(OlsDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<IReadOnlyList<string>> SyncAsync(
        long loadId, IReadOnlyList<long> keepIds, IReadOnlyList<NewLoadFile> newFiles,
        CancellationToken cancellationToken = default)
    {
        var existing = await _db.LoadFiles
            .Where(f => f.LoadId == (int)loadId)
            .ToListAsync(cancellationToken);

        var now = _clock.Now;

        foreach (var file in newFiles)
        {
            _db.LoadFiles.Add(new LoadFile
            {
                LoadId = (int)loadId,
                File = file.StoredName,
                MimeType = file.Extension,
                OrgName = file.OriginalName,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }

        // Listede id'si geçmeyen her mevcut dosya silinir.
        var removed = existing.Where(f => !keepIds.Contains(f.Id)).ToList();

        _db.LoadFiles.RemoveRange(removed);
        await _db.SaveChangesAsync(cancellationToken);

        return removed
            .Where(f => !string.IsNullOrWhiteSpace(f.File))
            .Select(f => f.File!)
            .ToList();
    }
}
