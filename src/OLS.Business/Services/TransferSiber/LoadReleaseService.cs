using Microsoft.EntityFrameworkCore;
using OLS.Business.Common;
using OLS.DataAccess.Context;
using OLS.DataAccess.Siber;

namespace OLS.Business.Services.TransferSiber;

/// <summary>
/// Teklifi (rezervasyonu) gerçek yüke dönüştürme —
/// <c>POST /transfer_to_siber/loadSave</c>.
/// olsold: <c>TransferSiberController::loadSave</c>
///
/// İki aşamalıdır:
/// <list type="number">
/// <item><b>Ön kontroller</b> — yerel kayıt uygun mu (yük zaten açılmış mı,
/// durumu Olumlu mu, Siber'e aktarılmış mı) ve yerel veriler Siber'deki
/// rezervasyonla birebir örtüşüyor mu.</item>
/// <item><b>Dönüşüm</b> — Siber'in saklı yordamları yükü açar; oluşan yük
/// numarası yerel <c>loads.load_number</c> alanına yazılır.</item>
/// </list>
///
/// Kaynak 17 ayrı alanı Siber'le karşılaştırıyor ve <b>hepsinde aynı</b> hata
/// mesajını döndürüyor: "Verileri Siberle eşleşmiyor lütfen önce sibere
/// aktarın". Bu, hangi alanın tutmadığını gizliyor; port hata mesajını
/// koruyup <b>hangi alanın</b> uyuşmadığını da ekler.
/// </summary>
public interface ILoadReleaseService
{
    bool IsConfigured { get; }

    Task<LoadReleaseResult> ReleaseAsync(
        string reservationId, long? userId, CancellationToken cancellationToken = default);
}

public sealed record LoadReleaseResult(
    bool Success, string Message, string? YukId = null, string? LoadNumber = null)
{
    public static LoadReleaseResult Fail(string message) => new(false, message);
}

public sealed class LoadReleaseService : ILoadReleaseService
{
    /// <summary>5 = Olumlu (teklif kabul edildi).</summary>
    private const int AcceptedStatusTypeId = 5;

    private readonly OlsDbContext _db;
    private readonly ISiberLoadReleaseRepository _siber;
    private readonly IClock _clock;

    public LoadReleaseService(
        OlsDbContext db, ISiberLoadReleaseRepository siber, IClock clock)
    {
        _db = db;
        _siber = siber;
        _clock = clock;
    }

    public bool IsConfigured => _siber.IsConfigured;

    public async Task<LoadReleaseResult> ReleaseAsync(
        string reservationId, long? userId, CancellationToken cancellationToken = default)
    {
        var load = await _db.Loads
            .FirstOrDefaultAsync(l => l.SiberId == reservationId, cancellationToken);

        if (load is null)
            return LoadReleaseResult.Fail("Kayıt Bulunamadı");

        if (!string.IsNullOrWhiteSpace(load.LoadNumber))
            return LoadReleaseResult.Fail("Bu yük zaten oluşturuldu.");

        if (load.StatusTypeId != AcceptedStatusTypeId)
            return LoadReleaseResult.Fail("Yük durumu Olumlu değil");

        if (load.TransferToSiber == 0 || string.IsNullOrWhiteSpace(load.SiberId))
            return LoadReleaseResult.Fail("Önce Teklif Oluşturun");

        var snapshot = await _siber.FindRezervasyonAsync(reservationId, cancellationToken);

        if (snapshot is null)
            return LoadReleaseResult.Fail(
                "Verileri Siberle eşleşmiyor lütfen önce sibere aktarın (rezervasyon bulunamadı)");

        if (await CompareAsync(load, snapshot, cancellationToken) is { } mismatch)
            return LoadReleaseResult.Fail(
                $"Verileri Siberle eşleşmiyor lütfen önce sibere aktarın ({mismatch})");

        if (await ValidateRequiredAsync(load, cancellationToken) is { } missing)
            return LoadReleaseResult.Fail(missing);

        var siberUserCode = userId is null
            ? null
            : await _db.Users.AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => u.SiberCode)
                .FirstOrDefaultAsync(cancellationToken);

        var released = await _siber.ReleaseAsync(reservationId, siberUserCode, cancellationToken);

        if (released is null)
            return LoadReleaseResult.Fail("Yük bilgileri oluşturulamadı.");

        load.LoadNumber = released.LoadNumber;
        load.UpdatedAt = _clock.Now;

        await _db.SaveChangesAsync(cancellationToken);

        return new LoadReleaseResult(
            true, "Yük başarıyla oluşturuldu", released.YukId, released.LoadNumber);
    }

    /// <summary>
    /// Kaynaktaki 17 karşılaştırma. Hepsi <c>strtoupper</c> ile büyük harfe
    /// çevrilip karşılaştırılıyor; port da harf duyarsız karşılaştırır.
    /// Uyuşmayan ilk alanın adı döner, hepsi tutuyorsa <c>null</c>.
    /// </summary>
    private async Task<string?> CompareAsync(
        DataAccess.Entities.Load load, SiberRezervasyonSnapshot siber,
        CancellationToken cancellationToken)
    {
        var romorkType = await CodeAsync(_db.RomorkTypes, load.RomorkTypeId, cancellationToken);
        var workType = await CodeAsync(_db.WorkTypes, load.WorkTypeId, cancellationToken);
        var instruction = await CodeAsync(_db.Instructions, load.InstructionId, cancellationToken);
        var loadingType = await CodeAsync(_db.LoadingTypes, load.LoadingTypeId, cancellationToken);
        var transferType = await CodeAsync(_db.LoadTransferTypes, load.LoadTransferTypeId, cancellationToken);

        var paymentType = await _db.PaymentTypes.AsNoTracking()
            .Where(p => p.Id == load.PaymentTypeId).Select(p => p.SiberId)
            .FirstOrDefaultAsync(cancellationToken);

        var statusType = await _db.StatusTypes.AsNoTracking()
            .Where(s => s.Id == load.StatusTypeId).Select(s => s.SiberId)
            .FirstOrDefaultAsync(cancellationToken);

        var department = await _db.Departments.AsNoTracking()
            .Where(d => d.Id == load.DepartmentId).Select(d => d.SiberId)
            .FirstOrDefaultAsync(cancellationToken);

        var customer = await AccountSiberIdAsync(load.CustomerId, cancellationToken);
        var freightCompany = await AccountSiberIdAsync(load.CompanyPayFreightId, cancellationToken);
        var sender = await AccountSiberIdAsync(load.SenderId, cancellationToken);
        var receiver = await AccountSiberIdAsync(load.ReceiverId, cancellationToken);

        var checks = new (string Field, string? Local, string? Remote)[]
        {
            ("romork tipi", romorkType, siber.IstenenRomorkCins),
            ("iş türü", workType, siber.IsTuru),
            ("talimat geliş şekli", instruction, siber.TalimatGelisSekli),
            ("yükleme tipi", loadingType, siber.YuklemeTip),
            ("yük tür kodu", transferType, siber.YukTurKod),
            ("ödeme şekli", paymentType, siber.OdemeSekliId),
            ("ön taşıma", load.FrontTransportationByUs.ToString(), siber.OnTasimaTarafimizdanYapilir),
            ("son taşıma", load.FinalTransportationByUs.ToString(), siber.SonTasimaTarafimizdanYapilir),
            ("müşteri", customer, siber.MusteriId),
            ("navlun firması", freightCompany, siber.NavlunFirmaId),
            ("gönderici", sender, siber.GondericiId),
            ("alıcı", receiver, siber.AliciId),
            ("durum", statusType, siber.DurumId),
            ("departman", department, siber.DepartmanId),
            ("yükleme ülkesi", load.DepartureCountryId?.ToString(), siber.YuklemeUlkeId),
            ("boşaltma ülkesi", load.TargetCountryId?.ToString(), siber.BosaltmaUlkeId),
            ("çalışma şekli", load.WayOfWorking.ToString(), siber.CalismaSekli),
        };

        foreach (var (field, local, remote) in checks)
        {
            if (!string.Equals(local?.Trim(), remote?.Trim(), StringComparison.OrdinalIgnoreCase))
                return field;
        }

        return null;
    }

    /// <summary>Kaynaktaki zorunlu alan listesi; ilk boş alanın mesajı döner.</summary>
    private async Task<string?> ValidateRequiredAsync(
        DataAccess.Entities.Load load, CancellationToken cancellationToken)
    {
        var hasContent = await _db.LoadContents
            .AnyAsync(c => c.LoadId == load.Id, cancellationToken);

        if (!hasContent)
            return "Yük içerikleri boş olamaz";

        var hasFinancialItem = await _db.LoadFinancialItems
            .AnyAsync(i => i.LoadId == load.Id, cancellationToken);

        if (!hasFinancialItem)
            return "Yük finansal kalemleri boş olamaz";

        if (load.WayOfWorking == 0)
            return "Çalışma şekli boş olamaz";

        return null;
    }

    private async Task<string?> AccountSiberIdAsync(int? accountId, CancellationToken cancellationToken) =>
        accountId is null
            ? null
            : await _db.Accounts.AsNoTracking()
                .Where(a => a.Id == accountId)
                .Select(a => a.SiberId)
                .FirstOrDefaultAsync(cancellationToken);

    private static async Task<string?> CodeAsync<T>(
        DbSet<T> set, int? id, CancellationToken cancellationToken)
        where T : class
    {
        if (id is null)
            return null;

        // Tanım tablolarının hepsinde Id + Code var ama ortak bir arayüz yok;
        // EF'in shadow property okuyucusu ile alınır.
        return await set.AsNoTracking()
            .Where(e => EF.Property<long>(e, "Id") == id)
            .Select(e => EF.Property<string?>(e, "Code"))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
