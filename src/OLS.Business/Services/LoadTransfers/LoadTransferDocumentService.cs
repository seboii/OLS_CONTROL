using Microsoft.EntityFrameworkCore;
using OLS.Business.Common;
using OLS.DataAccess.Context;
using OLS.DataAccess.Entities;
using OLS.DataAccess.Siber;

namespace OLS.Business.Services.LoadTransfers;

/// <summary>
/// Evrak Takibi — skn_yukevrak karşılığı. olsold'da hiç karşılığı yoktu (yeni
/// özellik): Yük'e bağlı, 10 sabit türden (bkz. EvrakTuru) hangi fiziksel
/// evraktan kaç orijinal/kopya çıkarıldığı ve kime/ne zaman teslim edildiği.
/// Paketler gibi tüm yük formunun parçası olarak DEĞİL, Hareketler
/// (LoadTransferMovement) gibi bağımsız, anlık kaydedilen bir alt-kaynak —
/// ama Hareketler'in aksine Siber'e de yazıyor (skn_yukevrak INSERT/UPDATE/
/// DELETE), çünkü fiziksel evrak çeklistinin gerçek kaynağı hâlâ Siber.
/// </summary>
public interface ILoadTransferDocumentService
{
    Task<IReadOnlyList<LoadTransferDocumentDto>> ListAsync(
        long loadTransferId, CancellationToken cancellationToken = default);

    Task<LoadTransferDocumentResult> SaveAsync(
        LoadTransferDocumentInput input, CancellationToken cancellationToken = default);

    Task<LoadTransferDocumentResult> UpdateAsync(
        long id, LoadTransferDocumentInput input, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default);
}

public sealed class LoadTransferDocumentInput
{
    public long LoadTransferId { get; set; }
    public long EvrakTuruId { get; set; }
    public string? DocumentNumber { get; set; }
    public DateOnly? Date { get; set; }
    public int? OriginalCount { get; set; }
    public int? CopyCount { get; set; }
    public string? DeliveredTo { get; set; }
    public DateOnly? DeliveredAt { get; set; }
    public string? Note { get; set; }
}

public sealed record LoadTransferDocumentResult(LoadTransferDocumentDto? Data, string? ErrorMessage)
{
    public bool IsSuccess => Data is not null;
}

public sealed class LoadTransferDocumentDto
{
    public long Id { get; init; }
    public long LoadTransferId { get; init; }
    public long? EvrakTuruId { get; init; }
    public string? EvrakTuruName { get; init; }
    public string? DocumentNumber { get; init; }
    public DateOnly? Date { get; init; }
    public int? OriginalCount { get; init; }
    public int? CopyCount { get; init; }
    public string? DeliveredTo { get; init; }
    public DateOnly? DeliveredAt { get; init; }
    public string? Note { get; init; }
    public DateTime? CreatedAt { get; init; }
}

public sealed class LoadTransferDocumentService : ILoadTransferDocumentService
{
    private readonly OlsDbContext _db;
    private readonly ISiberLoadRepository _siber;
    private readonly IClock _clock;

    public LoadTransferDocumentService(OlsDbContext db, ISiberLoadRepository siber, IClock clock)
    {
        _db = db;
        _siber = siber;
        _clock = clock;
    }

    public async Task<IReadOnlyList<LoadTransferDocumentDto>> ListAsync(
        long loadTransferId, CancellationToken cancellationToken = default) =>
        await _db.LoadTransferDocuments.AsNoTracking()
            .Where(d => d.LoadTransferId == loadTransferId)
            .OrderByDescending(d => d.CreatedAt)
            .Select(ProjectDocument())
            .ToListAsync(cancellationToken);

    public async Task<LoadTransferDocumentResult> SaveAsync(
        LoadTransferDocumentInput input, CancellationToken cancellationToken = default)
    {
        if (!_siber.IsConfigured)
            return new LoadTransferDocumentResult(null, "Siber bağlantısı yapılandırılmamış.");

        // LoadTransfer.LoadTransferId (string) — bu satırın KENDİ Siber yük kimliği
        // (skn_yuk.yukid). Aynı isimdeki yerel Id (long) ile karıştırılmamalı.
        var siberYukId = await _db.LoadTransfers.AsNoTracking()
            .Where(t => t.Id == input.LoadTransferId)
            .Select(t => t.LoadTransferId)
            .FirstOrDefaultAsync(cancellationToken);

        if (siberYukId is null)
            return new LoadTransferDocumentResult(null, "Yük bulunamadı veya henüz Siber'e aktarılmamış");

        var evrakTuru = await _db.EvrakTurus.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == input.EvrakTuruId, cancellationToken);

        if (evrakTuru is null || !int.TryParse(evrakTuru.Code, out var sirano))
            return new LoadTransferDocumentResult(null, "Evrak türü bulunamadı");

        var now = _clock.Now;
        var evrakId = await _siber.GenerateYukEvrakIdAsync(cancellationToken);

        await _siber.InsertYukEvrakAsync(new SiberYukEvrak
        {
            YukEvrakId = evrakId.ToString(),
            YukId = siberYukId,
            Sirano = sirano,
            EvrakAd = evrakTuru.Name,
            EvrakNo = input.DocumentNumber,
            Tarih = input.Date?.ToDateTime(TimeOnly.MinValue),
            OrjinalAdet = input.OriginalCount,
            KopyaAdet = input.CopyCount,
            TeslimAlan = input.DeliveredTo,
            TeslimTarih = input.DeliveredAt?.ToDateTime(TimeOnly.MinValue),
            Aciklama = input.Note,
        }, cancellationToken);

        var document = new LoadTransferDocument
        {
            Yukevrakid = evrakId.ToString(),
            LoadTransferId = input.LoadTransferId,
            EvrakTuruId = input.EvrakTuruId,
            DocumentNumber = input.DocumentNumber,
            Date = input.Date,
            OriginalCount = input.OriginalCount,
            CopyCount = input.CopyCount,
            DeliveredTo = input.DeliveredTo,
            DeliveredAt = input.DeliveredAt,
            Note = input.Note,
            CreatedAt = now,
            UpdatedAt = now,
        };

        _db.LoadTransferDocuments.Add(document);
        await _db.SaveChangesAsync(cancellationToken);

        return new LoadTransferDocumentResult(await SingleAsync(document.Id, cancellationToken), null);
    }

    public async Task<LoadTransferDocumentResult> UpdateAsync(
        long id, LoadTransferDocumentInput input, CancellationToken cancellationToken = default)
    {
        if (!_siber.IsConfigured)
            return new LoadTransferDocumentResult(null, "Siber bağlantısı yapılandırılmamış.");

        var document = await _db.LoadTransferDocuments
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        if (document?.Yukevrakid is null)
            return new LoadTransferDocumentResult(null, "Evrak kaydı bulunamadı");

        var evrakTuru = await _db.EvrakTurus.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == input.EvrakTuruId, cancellationToken);

        if (evrakTuru is null || !int.TryParse(evrakTuru.Code, out var sirano))
            return new LoadTransferDocumentResult(null, "Evrak türü bulunamadı");

        await _siber.UpdateYukEvrakAsync(new SiberYukEvrak
        {
            YukEvrakId = document.Yukevrakid,
            Sirano = sirano,
            EvrakAd = evrakTuru.Name,
            EvrakNo = input.DocumentNumber,
            Tarih = input.Date?.ToDateTime(TimeOnly.MinValue),
            OrjinalAdet = input.OriginalCount,
            KopyaAdet = input.CopyCount,
            TeslimAlan = input.DeliveredTo,
            TeslimTarih = input.DeliveredAt?.ToDateTime(TimeOnly.MinValue),
            Aciklama = input.Note,
        }, cancellationToken);

        document.EvrakTuruId = input.EvrakTuruId;
        document.DocumentNumber = input.DocumentNumber;
        document.Date = input.Date;
        document.OriginalCount = input.OriginalCount;
        document.CopyCount = input.CopyCount;
        document.DeliveredTo = input.DeliveredTo;
        document.DeliveredAt = input.DeliveredAt;
        document.Note = input.Note;
        document.UpdatedAt = _clock.Now;

        await _db.SaveChangesAsync(cancellationToken);

        return new LoadTransferDocumentResult(await SingleAsync(id, cancellationToken), null);
    }

    public async Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var document = await _db.LoadTransferDocuments
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        if (document is null)
            return false;

        if (document.Yukevrakid is not null)
            await _siber.DeleteYukEvrakAsync(document.Yukevrakid, cancellationToken);

        _db.LoadTransferDocuments.Remove(document);
        await _db.SaveChangesAsync(cancellationToken);

        return true;
    }

    private async Task<LoadTransferDocumentDto?> SingleAsync(long id, CancellationToken cancellationToken) =>
        await _db.LoadTransferDocuments.AsNoTracking()
            .Where(d => d.Id == id)
            .Select(ProjectDocument())
            .FirstOrDefaultAsync(cancellationToken);

    private System.Linq.Expressions.Expression<Func<LoadTransferDocument, LoadTransferDocumentDto>>
        ProjectDocument() =>
        d => new LoadTransferDocumentDto
        {
            Id = d.Id,
            LoadTransferId = d.LoadTransferId,
            EvrakTuruId = d.EvrakTuruId,
            EvrakTuruName = _db.EvrakTurus.Where(e => e.Id == d.EvrakTuruId).Select(e => e.Name).FirstOrDefault(),
            DocumentNumber = d.DocumentNumber,
            Date = d.Date,
            OriginalCount = d.OriginalCount,
            CopyCount = d.CopyCount,
            DeliveredTo = d.DeliveredTo,
            DeliveredAt = d.DeliveredAt,
            Note = d.Note,
            CreatedAt = d.CreatedAt,
        };
}
