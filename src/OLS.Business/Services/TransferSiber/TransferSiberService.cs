using Microsoft.EntityFrameworkCore;
using OLS.Business.Common;
using OLS.DataAccess.Context;
using OLS.DataAccess.Siber;

namespace OLS.Business.Services.TransferSiber;

/// <summary>
/// Teklifi Siber'e rezervasyon olarak aktarır.
/// olsold: <c>Front\TransferSiber\TransferSiberController::save</c>
///
/// Bu adım teklif→yük dönüşümünün ÖN KOŞULUDUR: <c>loads.siber_id</c>,
/// <c>transfer_to_siber</c> ve <c>reservation_number</c> alanlarını burası doldurur.
///
/// İki mod:
///   - <c>transfer_to_siber = 0</c> → yeni rezervasyon (skn_rezervasyon insert,
///     numara max+1)
///   - <c>transfer_to_siber = 1</c> → mevcut rezervasyon güncellenir
///     (yük oluşmuşsa güncelleme engellenir)
///
/// Alt kayıtlar (içerik → skn_rezervasyonyukkoli, finansal kalem →
/// skn_rezervasyontarife) siber_id'lerine göre insert veya update edilir ve
/// üretilen Siber kimliği yerel satıra geri yazılır.
/// </summary>
public interface ITransferSiberService
{
    Task<TransferSiberResult> TransferOfferAsync(
        long loadId, long currentUserId, CancellationToken cancellationToken = default);
}

public sealed record TransferSiberResult(
    string? SiberId, int? ReservationNumber, string? ErrorMessage)
{
    public bool IsSuccess => ErrorMessage is null;
    public static TransferSiberResult Fail(string message) => new(null, null, message);
    public static TransferSiberResult Ok(string siberId, int no) => new(siberId, no, null);
}

public sealed class TransferSiberService : ITransferSiberService
{
    private readonly OlsDbContext _db;
    private readonly ISiberReservationRepository _siber;
    private readonly IClock _clock;

    public TransferSiberService(
        OlsDbContext db, ISiberReservationRepository siber, IClock clock)
    {
        _db = db;
        _siber = siber;
        _clock = clock;
    }

    public async Task<TransferSiberResult> TransferOfferAsync(
        long loadId, long currentUserId, CancellationToken cancellationToken = default)
    {
        if (!_siber.IsConfigured)
            return TransferSiberResult.Fail("Siber bağlantısı yapılandırılmamış.");

        var load = await _db.Loads.FirstOrDefaultAsync(l => l.Id == loadId, cancellationToken);
        if (load is null)
            return TransferSiberResult.Fail("Teklif bulunamadı");

        // Yük oluşmuş teklif güncellenemez.
        if (load.TransferToSiber == 1 && load.LoadNumber is not null)
            return TransferSiberResult.Fail("Yük oluşturulmuş teklifde güncelleme yapamazsınız.");

        var refs = await LoadReferencesAsync(load, cancellationToken);

        if (ValidateRequired(load, refs) is { } missing)
            return TransferSiberResult.Fail(missing);

        var now = _clock.Now;
        var isUpdate = load.TransferToSiber == 1 && load.SiberId is not null;

        var rezervasyonId = isUpdate
            ? load.SiberId!
            : (await _siber.GenerateRezervasyonIdAsync(cancellationToken)).ToString();

        var rezervasyonNo = isUpdate
            ? load.ReservationNumber is not null && int.TryParse(load.ReservationNumber, out var existing)
                ? existing
                : 0
            : await _siber.NextRezervasyonNoAsync(cancellationToken);

        var reservation = new SiberRezervasyonYaz
        {
            RezervasyonId = rezervasyonId,
            RezervasyonNo = rezervasyonNo,
            TalimatGelisSekli = refs.InstructionCode,
            IstenenRomorkCins = refs.RomorkTypeCode,
            IsTuru = refs.WorkTypeCode,
            YuklemeTip = refs.LoadingTypeCode,
            YukTurKod = refs.LoadTransferTypeCode,
            PazarlamaBildirimTarih = ToDateTime(load.MarketingNotificationDate),
            TalimatGelisTarih = ToDateTime(load.OfferDate),
            GecerlilikTarih = ToDateTime(load.OfferValidityDate),
            OdemeSekliId = refs.PaymentTypeSiberId,
            OnTasimaTarafimizdanYapilir = load.FrontTransportationByUs,
            SonTasimaTarafimizdanYapilir = load.FinalTransportationByUs,
            MusteriId = refs.CustomerSiberId,
            NavlunFirmaId = refs.CompanyPayFreightSiberId,
            GondericiId = refs.SenderSiberId,
            AliciId = refs.ReceiverSiberId,
            DurumId = refs.StatusTypeSiberId,
            MusteriTemsilcisi = refs.CustomerRepName,
            SatisTemsilcisiKod = refs.SalesRepCode,
            DepartmanId = refs.DepartmentSiberId,
            Aciklama = load.Description,
            Yil = now.Year,
            YuklemeUlkeId = load.DepartureCountryId?.ToString(),
            BosaltmaUlkeId = load.TargetCountryId?.ToString(),
            CalismaSekli = load.WayOfWorking,
            InsTime = now,
            InsUser = refs.CurrentUserSiberCode,
        };

        if (isUpdate)
            await _siber.UpdateRezervasyonAsync(reservation, cancellationToken);
        else
            await _siber.InsertRezervasyonAsync(reservation, cancellationToken);

        await TransferContentsAsync(load, rezervasyonId, cancellationToken);
        await TransferFinancialItemsAsync(load, rezervasyonId, now, cancellationToken);

        load.TransferToSiber = 1;
        load.SiberId = rezervasyonId;
        load.ReservationNumber = rezervasyonNo.ToString();
        load.UpdatedAt = now;
        await _db.SaveChangesAsync(cancellationToken);

        return TransferSiberResult.Ok(rezervasyonId, rezervasyonNo);
    }

    /// <summary>Yük içerikleri → skn_rezervasyonyukkoli (siber_id'ye göre insert/update).</summary>
    private async Task TransferContentsAsync(
        DataAccess.Entities.Load load, string rezervasyonId, CancellationToken cancellationToken)
    {
        var contents = await _db.LoadContents
            .Where(c => c.LoadId == load.Id)
            .ToListAsync(cancellationToken);

        foreach (var content in contents)
        {
            var productSiberId = content.ProductTypeId is null ? null : await _db.ProductTypes
                .Where(p => p.Id == content.ProductTypeId)
                .Select(p => new { p.SiberId, p.Name }).FirstOrDefaultAsync(cancellationToken);

            var caseSiberId = content.CaseTypeId is null ? null : await _db.CaseTypes
                .Where(c => c.Id == content.CaseTypeId)
                .Select(c => c.SiberId).FirstOrDefaultAsync(cancellationToken);

            var exists = content.SiberId is not null &&
                         await _siber.YukKoliExistsAsync(content.SiberId, cancellationToken);

            var koliId = exists
                ? content.SiberId!
                : (await _siber.GenerateYukKoliIdAsync(cancellationToken)).ToString();

            var koli = new SiberRezervasyonYukKoli
            {
                RezYukKoliId = koliId,
                RezervasyonId = rezervasyonId,
                KapAdet = content.Quantity ?? 0,
                // DİKKAT: olsold en/boy/yükseklik sırasını width/height/length olarak
                // yazıyor — yani 'boy' alanına height, 'yukseklik' alanına length
                // gidiyor. Kaynakla aynı kalması için birebir korundu.
                En = content.Width ?? 0,
                Boy = content.Height ?? 0,
                Yukseklik = content.Length ?? 0,
                MalCinsId = productSiberId?.SiberId,
                KapId = caseSiberId,
                TurkceTanim = productSiberId?.Name,
                Hacim = content.Volume ?? 0,
                BurutAgirlik = content.GrossWeight ?? 0,
                NetAgirlik = content.NetWeight ?? 0,
                Lademetre = content.Lademeter ?? 0,
                // Ters mantık: stackable = 1 ise istiflenemez = 0
                Istiflenemez = content.Stackable == 1 ? 0 : 1,
            };

            if (exists)
                await _siber.UpdateRezervasyonYukKoliAsync(koli, cancellationToken);
            else
                await _siber.InsertRezervasyonYukKoliAsync(koli, cancellationToken);

            content.SiberId = koliId;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Finansal kalemler → skn_rezervasyontarife.</summary>
    private async Task TransferFinancialItemsAsync(
        DataAccess.Entities.Load load, string rezervasyonId, DateTime now,
        CancellationToken cancellationToken)
    {
        var items = await _db.LoadFinancialItems
            .Where(f => f.LoadId == load.Id)
            .ToListAsync(cancellationToken);

        foreach (var item in items)
        {
            var currencyCode = item.Currency is null ? null : await _db.Currencies
                .Where(c => c.Id == item.Currency)
                .Select(c => c.Code).FirstOrDefaultAsync(cancellationToken);

            var itemSiberId = item.Item is null ? null : await _db.FinancialItems
                .Where(f => f.Id == item.Item)
                .Select(f => f.SiberId).FirstOrDefaultAsync(cancellationToken);

            var accountSiberId = item.AccountId is null ? null : await _db.Accounts
                .Where(a => a.Id == item.AccountId)
                .Select(a => a.SiberId).FirstOrDefaultAsync(cancellationToken);

            var transportCode = item.TransportTypeId is null ? null : await _db.TransportTypes
                .Where(t => t.Id == item.TransportTypeId)
                .Select(t => t.Code).FirstOrDefaultAsync(cancellationToken);

            var exists = item.SiberId is not null &&
                         await _siber.TarifeExistsAsync(item.SiberId, cancellationToken);

            var tarifeId = exists
                ? item.SiberId!
                : (await _siber.GenerateTarifeIdAsync(cancellationToken)).ToString();

            var tarife = new SiberRezervasyonTarife
            {
                RezervasyonTarifeId = tarifeId,
                RezervasyonId = rezervasyonId,
                Tarih = now,
                Miktar = item.Quantity ?? 0,
                AlisDovizKod = currencyCode,
                AlisBirimTutar = item.NetPrice ?? 0,
                AlisToplamTutar = item.TotalPrice ?? 0,
                KalemId = itemSiberId,
                AlisFirmaId = accountSiberId,
                TasimaSekli = transportCode,
            };

            if (exists)
                await _siber.UpdateRezervasyonTarifeAsync(tarife, cancellationToken);
            else
                await _siber.InsertRezervasyonTarifeAsync(tarife, cancellationToken);

            item.SiberId = tarifeId;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private sealed record OfferRefs(
        string? InstructionCode, string? RomorkTypeCode, string? WorkTypeCode,
        string? LoadingTypeCode, string? LoadTransferTypeCode, string? PaymentTypeSiberId,
        string? CustomerSiberId, string? CompanyPayFreightSiberId, string? SenderSiberId,
        string? ReceiverSiberId, string? StatusTypeSiberId, string? DepartmentSiberId,
        string? CustomerRepName, string? SalesRepCode, string? CurrentUserSiberCode);

    private async Task<OfferRefs> LoadReferencesAsync(
        DataAccess.Entities.Load load, CancellationToken cancellationToken)
    {
        var chargePeople = await _db.LoadChargePeople.AsNoTracking()
            .Where(p => p.LoadId == (int)load.Id)
            .OrderBy(p => p.Id)
            .Join(_db.Users, p => p.UserId, u => (int)u.Id,
                (p, u) => new { u.SiberName, u.SiberCode })
            .ToListAsync(cancellationToken);

        return new OfferRefs(
            await CodeAsync(_db.Instructions.Where(i => i.Id == load.InstructionId).Select(i => i.Code), cancellationToken),
            await CodeAsync(_db.RomorkTypes.Where(r => r.Id == load.RomorkTypeId).Select(r => r.Code), cancellationToken),
            await CodeAsync(_db.WorkTypes.Where(w => w.Id == load.WorkTypeId).Select(w => w.Code), cancellationToken),
            await CodeAsync(_db.LoadingTypes.Where(t => t.Id == load.LoadingTypeId).Select(t => t.Code), cancellationToken),
            await CodeAsync(_db.LoadTransferTypes.Where(t => t.Id == load.LoadTransferTypeId).Select(t => t.Code), cancellationToken),
            await CodeAsync(_db.PaymentTypes.Where(p => p.Id == load.PaymentTypeId).Select(p => p.SiberId), cancellationToken),
            await CodeAsync(_db.Accounts.Where(a => a.Id == load.CustomerId).Select(a => a.SiberId), cancellationToken),
            await CodeAsync(_db.Accounts.Where(a => a.Id == load.CompanyPayFreightId).Select(a => a.SiberId), cancellationToken),
            await CodeAsync(_db.Accounts.Where(a => a.Id == load.SenderId).Select(a => a.SiberId), cancellationToken),
            await CodeAsync(_db.Accounts.Where(a => a.Id == load.ReceiverId).Select(a => a.SiberId), cancellationToken),
            await CodeAsync(_db.StatusTypes.Where(s => s.Id == load.StatusTypeId).Select(s => s.SiberId), cancellationToken),
            await CodeAsync(_db.Departments.Where(d => d.Id == load.DepartmentId).Select(d => d.SiberId), cancellationToken),
            chargePeople.ElementAtOrDefault(0)?.SiberName,
            chargePeople.ElementAtOrDefault(1)?.SiberCode,
            chargePeople.ElementAtOrDefault(0)?.SiberCode);
    }

    private static async Task<string?> CodeAsync(
        IQueryable<string?> query, CancellationToken cancellationToken) =>
        await query.AsNoTracking().FirstOrDefaultAsync(cancellationToken);

    /// <summary>olsold'daki zorunlu alan listesi; mesajlar birebir korunur.</summary>
    private static string? ValidateRequired(DataAccess.Entities.Load load, OfferRefs r)
    {
        if (r.PaymentTypeSiberId is null) return "Ödeme şekli boş olamaz";
        if (load.FrontTransportationByUs < 0) return "Ön taşıma tarafımızdan yapılır boş olamaz";
        if (load.FinalTransportationByUs < 0) return "Son taşıma tarafımızdan yapılır boş olamaz";
        if (r.CustomerSiberId is null) return "Müşteri boş olamaz";
        if (r.StatusTypeSiberId is null) return "Durum boş olamaz";
        if (r.CustomerRepName is null) return "Müşteri temsilcisi boş olamaz";
        if (r.SalesRepCode is null) return "Satış temsilcisi kodu boş olamaz";
        if (r.DepartmentSiberId is null) return "Departman boş olamaz";

        return null;
    }

    private static DateTime? ToDateTime(DateOnly? date) => date?.ToDateTime(TimeOnly.MinValue);
}
