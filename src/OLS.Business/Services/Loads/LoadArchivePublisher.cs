using Microsoft.EntityFrameworkCore;
using OLS.Business.Services.Authorization;
using OLS.DataAccess.Context;
using OLS.DataAccess.Siber;

namespace OLS.Business.Services.Loads;

/// <summary>
/// Uygulamadan yüklenen yük eklerini SİBER ARŞİVİNE de gönderir.
///
/// Doğru kaydı ve modül kodunu bulmak bu katmanın işi: arşiv Siber'de
/// <c>modulid</c> ile bağlanıyor ve bu, teklif için rezervasyonid, yük için
/// yukid demek. Modül kodu da yükün iş türüne göre değişiyor
/// (bkz. SiberArchiveWriter).
///
/// HATA YUTULMAZ ama akış da durdurulmaz: dosya zaten uygulamanın kendi
/// deposuna kaydedilmiş durumda. Siber'e yazılamayan dosya için çağıran taraf
/// uyarı logluyor; kullanıcının yüklemesi boşa gitmiyor.
/// </summary>
public interface ILoadArchivePublisher
{
    /// <summary>Siber arşivine yazılabilen dosya sayısını döner.</summary>
    Task<int> PushAsync(
        long? loadId, long? loadTransferId,
        IReadOnlyList<(string Name, byte[] Content)> files,
        CancellationToken cancellationToken = default);
}

public sealed class LoadArchivePublisher : ILoadArchivePublisher
{
    private readonly OlsDbContext _db;
    private readonly ISiberArchiveWriter _writer;
    private readonly ICurrentUser _currentUser;

    public LoadArchivePublisher(
        OlsDbContext db, ISiberArchiveWriter writer, ICurrentUser currentUser)
    {
        _db = db;
        _writer = writer;
        _currentUser = currentUser;
    }

    public async Task<int> PushAsync(
        long? loadId, long? loadTransferId,
        IReadOnlyList<(string Name, byte[] Content)> files,
        CancellationToken cancellationToken = default)
    {
        if (files.Count == 0 || !_writer.IsConfigured)
            return 0;

        var target = await ResolveTargetAsync(loadId, loadTransferId, cancellationToken);
        if (target is null)
            return 0;

        var userCode = _currentUser.Id is { } id
            ? await _db.Users.AsNoTracking()
                .Where(u => u.Id == id).Select(u => u.SiberCode)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        var written = 0;

        foreach (var (name, content) in files)
        {
            var arsivId = await _writer.UploadAsync(new SiberArchiveUpload
            {
                ModulId = target.Value.ModulId,
                ModulKod = target.Value.ModulKod,
                FileName = name,
                Content = content,
                UserCode = userCode,
            }, cancellationToken);

            if (arsivId is not null)
                written++;
        }

        return written;
    }

    /// <summary>
    /// Dosyanın bağlanacağı Siber kaydı ve modül kodu.
    ///
    /// YÜK ÖNCELİKLİ: teklifi olan bir yükte dosya teklife de bağlanabilirdi,
    /// ama Siber'de evrak yükün üzerinde aranıyor (24.863 yük arşivi / 3.685
    /// teklif arşivi). Yük kimliği varsa oraya yazılır.
    /// </summary>
    private async Task<(string ModulId, string ModulKod)?> ResolveTargetAsync(
        long? loadId, long? loadTransferId, CancellationToken cancellationToken)
    {
        if (loadTransferId is { } transferId)
        {
            var transfer = await _db.LoadTransfers.AsNoTracking()
                .Where(t => t.Id == transferId)
                .Select(t => new { t.LoadTransferId, t.WorkType })
                .FirstOrDefaultAsync(cancellationToken);

            if (transfer?.LoadTransferId is { } yukId)
                return (yukId, await ModulKodAsync(transfer.WorkType, cancellationToken));
        }

        if (loadId is { } offerId)
        {
            // Teklifin yükü varsa evrak YÜKE bağlanır (Siber'in kendi deseni).
            var offer = await _db.Loads.AsNoTracking()
                .Where(l => l.Id == offerId)
                .Select(l => new { l.SiberId, l.LoadNumber })
                .FirstOrDefaultAsync(cancellationToken);

            if (offer is null)
                return null;

            if (offer.LoadNumber is { } loadNumber)
            {
                var transfer = await _db.LoadTransfers.AsNoTracking()
                    .Where(t => t.LoadNumberWorkType == loadNumber)
                    .Select(t => new { t.LoadTransferId, t.WorkType })
                    .FirstOrDefaultAsync(cancellationToken);

                if (transfer?.LoadTransferId is { } yukId)
                    return (yukId, await ModulKodAsync(transfer.WorkType, cancellationToken));
            }

            if (offer.SiberId is { } rezervasyonId)
                return (rezervasyonId, SiberArchiveWriter.ReservationModulKod);
        }

        return null;
    }

    /// <summary>
    /// Modül kodu yükün İŞ TÜRÜ koduna bağlı; yerel work_types.code Siber'in
    /// isturu değeriyle aynı (0=EX, 1=IM, 2=TR).
    /// </summary>
    private async Task<string> ModulKodAsync(int? workTypeId, CancellationToken cancellationToken)
    {
        if (workTypeId is not { } id)
            return SiberArchiveWriter.ModulKodForWorkType(null);

        var code = await _db.WorkTypes.AsNoTracking()
            .Where(w => w.Id == id)
            .Select(w => w.Code)
            .FirstOrDefaultAsync(cancellationToken);

        return SiberArchiveWriter.ModulKodForWorkType(
            int.TryParse(code, out var parsed) ? parsed : null);
    }
}
