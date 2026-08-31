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
    /// <summary>load_charge_people.user_type sözleşmesi (bkz. LoadChargePerson).</summary>
    private const int OperationOfficerType = 1;
    private const int SalesRepType = 2;

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

        var refs = await LoadReferencesAsync(load, currentUserId, cancellationToken);

        var hasContents = await _db.LoadContents.AnyAsync(c => c.LoadId == load.Id, cancellationToken);
        var hasFinancialItems = await _db.LoadFinancialItems.AnyAsync(f => f.LoadId == load.Id, cancellationToken);

        if (ValidateRequired(load, refs, hasContents, hasFinancialItems) is { } missing)
            return TransferSiberResult.Fail(missing);

        if (await ValidateSiberReferencesAsync(load, refs, cancellationToken) is { } invalidRef)
            return TransferSiberResult.Fail(invalidRef);

        if (await ValidateFinancialItemsExistInSiberAsync(load, cancellationToken) is { } invalidItem)
            return TransferSiberResult.Fail(invalidItem);

        var now = _clock.Now;
        var isUpdate = load.TransferToSiber == 1 && load.SiberId is not null;

        var rezervasyonId = isUpdate
            ? load.SiberId!
            : (await _siber.GenerateRezervasyonIdAsync(cancellationToken)).ToString();

        // Güncellemede numara zaten var (değişmez); yeni aktarımda numara, INSERT ile
        // AYNI transaction+kilit altında InsertRezervasyonWithLockedNumberAsync
        // tarafından üretilir — burada yalnızca yer tutucu (0) geçilir. Bkz. metodun
        // XML açıklaması (Siber Entegrasyon Raporu risk #3).
        var rezervasyonNo = isUpdate
            ? load.ReservationNumber is not null && int.TryParse(load.ReservationNumber, out var existing)
                ? existing
                : 0
            : 0;

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
            // Olumlu'ya çekilme tarihi — yerelde loads.approval_date olarak
            // damgalanır (bkz. LoadWriteService.ResolveApprovalDate), Siber'de
            // onaytarih sütununa karşılık gelir.
            OnayTarih = ToDateTime(load.ApprovalDate),
            InsTime = now,
            InsUser = refs.InsUserSiberCode,
        };

        // PostgreSQL↔Siber arası gerçek dağıtık transaction (2PC) yok — iki farklı
        // veritabanı motoru arasında pratikte kurulamaz. Bunun yerine YEREL taraf tek
        // bir transaction'a alınır ve yalnızca TÜM Siber yazmaları bittikten SONRA
        // commit edilir: herhangi bir adım (Siber çağrısı veya son SaveChanges) hata
        // verirse yerel taraf TAMAMEN geri alınır. Sonuç: Siber'de en kötü ihtimalle
        // yetim (hiçbir yerel kayda bağlı olmayan) satırlar kalabilir — zararsız,
        // sonraki deneme yeni kimliklerle temiz başlar — ama yerel taraf ASLA var
        // olmayan/eksik bir Siber kaydına işaret eden tutarsız bir duruma düşmez
        // (önceki hâlde art arda birden fazla SaveChangesAsync çağrısı arada Siber
        // I/O'su hata verirse bu garantiyi vermiyordu).
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            if (isUpdate)
                await _siber.UpdateRezervasyonAsync(reservation, cancellationToken);
            else
                rezervasyonNo = await _siber.InsertRezervasyonWithLockedNumberAsync(reservation, cancellationToken);

            await TransferContentsAsync(load, rezervasyonId, cancellationToken);
            await TransferFinancialItemsAsync(load, rezervasyonId, now, cancellationToken);

            load.TransferToSiber = 1;
            load.SiberId = rezervasyonId;
            load.ReservationNumber = rezervasyonNo.ToString();
            load.UpdatedAt = now;
            await _db.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

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
        // SaveChanges burada YAPILMAZ — TransferOfferAsync'in tek, dış transaction'lı
        // final SaveChangesAsync'i tüm bu satırların siber_id damgalarını da kapsar.
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
                // Yön: kalemin alış mı satış mı olduğu Siber'de HANGİ sütun grubunun
                // dolacağını belirler (bkz. SiberRezervasyonTarife.Buysell). Yön boşsa
                // olsold'daki varsayılan davranışa (alış) düşülür.
                Buysell = item.Buysell ?? 1,
                DovizKod = currencyCode,
                BirimTutar = item.NetPrice ?? 0,
                ToplamTutar = item.TotalPrice ?? 0,
                KalemId = itemSiberId,
                FirmaId = accountSiberId,
                TasimaSekli = transportCode,
            };

            if (exists)
                await _siber.UpdateRezervasyonTarifeAsync(tarife, cancellationToken);
            else
                await _siber.InsertRezervasyonTarifeAsync(tarife, cancellationToken);

            item.SiberId = tarifeId;
        }
        // SaveChanges burada YAPILMAZ — bkz. TransferContentsAsync'deki aynı not.
    }

    private sealed record OfferRefs(
        string? InstructionCode, string? RomorkTypeCode, string? WorkTypeCode,
        string? LoadingTypeCode, string? LoadTransferTypeCode, string? PaymentTypeSiberId,
        string? CustomerSiberId, string? CompanyPayFreightSiberId, string? SenderSiberId,
        string? ReceiverSiberId, string? StatusTypeSiberId, string? DepartmentSiberId,
        string? CustomerRepName, string? CustomerRepCode, string? SalesRepCode,
        string? InsUserSiberCode);

    /// <summary>
    /// <paramref name="currentUserId"/>: olsold <c>insuser</c>'ı <c>Auth::user()</c>'dan
    /// (o an işlemi yapan kullanıcı) alır — teklifin 1. görevlisinden DEĞİL. Daha önce
    /// burada yanlışlıkla görevli[0]'ın kodu kullanılıyordu.
    /// </summary>
    private async Task<OfferRefs> LoadReferencesAsync(
        DataAccess.Entities.Load load, long currentUserId, CancellationToken cancellationToken)
    {
        // GÖREVLİ TİPİNE GÖRE eşleşir. Eskiden sıraya (ElementAt 0/1) güveniliyordu:
        // yazma tarafı Operasyon'u ilk eklediği sürece çalışıyordu, ama satır sırası
        // bir kez değişse (senkron, elle düzeltme, çoklu satış temsilcisi) iki alan
        // sessizce yer değiştirirdi. user_type sözleşmesi açık: 1 = Operasyon
        // Yetkilisi → musteritemsilcisi, 2 = Satış Temsilcisi → satistemsilcisikod.
        var chargePeople = await _db.LoadChargePeople.AsNoTracking()
            .Where(p => p.LoadId == (int)load.Id)
            .OrderBy(p => p.Id)
            .Join(_db.Users, p => p.UserId, u => (int)u.Id,
                (p, u) => new { p.UserType, u.SiberName, u.SiberCode })
            .ToListAsync(cancellationToken);

        var operationOfficer = chargePeople.FirstOrDefault(p => p.UserType == OperationOfficerType);
        var salesRep = chargePeople.FirstOrDefault(p => p.UserType == SalesRepType);

        var insUserSiberCode = await _db.Users.AsNoTracking()
            .Where(u => u.Id == currentUserId)
            .Select(u => u.SiberCode)
            .FirstOrDefaultAsync(cancellationToken);

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
            operationOfficer?.SiberName,
            operationOfficer?.SiberCode,
            salesRep?.SiberCode,
            // insuser: işlemi yapan kullanıcı. Onun Siber karşılığı yoksa (sistem
            // hesabı) alan boş kalmasın diye operasyon yetkilisinin koduna düşülür —
            // Siber'in kendi kayıtlarında insuser 19023/19024 satırda dolu, boş
            // bırakmak kaydı oradaki alışılmış hâlden ayırırdı.
            insUserSiberCode ?? operationOfficer?.SiberCode);
    }

    private static async Task<string?> CodeAsync(
        IQueryable<string?> query, CancellationToken cancellationToken) =>
        await query.AsNoTracking().FirstOrDefaultAsync(cancellationToken);

    /// <summary>
    /// olsold'daki zorunlu alan listesi ($fields dizisi, TransferSiberController::save);
    /// mesajlar VE SIRA birebir korunur. Önceki sürüm bu 21 kontrolden yalnızca 8'ini
    /// içeriyordu (ör. talimat/römork/iş türü/tarihler/gönderici/alıcı hiç
    /// doğrulanmıyordu) — eksik alanlarla teklif sessizce Siber'e aktarılabiliyordu.
    /// Ön/son taşıma ve çalışma şekli olsold'da da <c>=== null || === ''</c> ile
    /// kontrol ediliyor; bu üç alan şemada NOT NULL + default 0 olduğundan kontrol
    /// kaynakta da fiilen hiç tetiklenmiyor (0, null/''e eşit değil) — burada da
    /// aynı şekilde atlanır (davranış birebir, ölü kod tekrar edilmedi).
    /// </summary>
    /// <summary>
    /// Siber'e gönderilecek TÜM yabancı anahtarları yazmadan önce doğrular.
    ///
    /// KÖK ÇÖZÜM: Yerel referans tabloları (departman, cari, ödeme şekli...) Siber'de
    /// karşılığı olmayan değerler taşıyabiliyor — DbSeeder'dan kalan tohum kayıtları
    /// ya da Siber'de sonradan silinmiş satırlar. Böyle bir değer gönderildiğinde
    /// Siber'in FK kısıtı INSERT'i reddediyor ve bu, kullanıcıya "beklenmeyen hata"
    /// olarak yansıyan işlenmemiş bir 500'e dönüşüyordu. Hata her seferinde farklı
    /// bir kolonda çıktığı için tek tek keşfediliyordu (departmanid, kalemid, ...).
    ///
    /// Kontrol edilen kolonlar, Siber'in skn_rezervasyon üzerindeki GERÇEK FK
    /// kısıtlarından alındı (sys.foreign_keys ile çıkarıldı): departmanid ve
    /// musteriid. Diğerleri (gönderici/alıcı/navlun firma) bu tabloda FK ile
    /// bağlı değil, ama yük aşamasında bağlı olduğu için onlar da kontrol edilir —
    /// hatayı bir adım önce ve anlaşılır biçimde yakalamak için.
    /// </summary>
    private async Task<string?> ValidateSiberReferencesAsync(
        DataAccess.Entities.Load load, OfferRefs refs, CancellationToken cancellationToken)
    {
        (string Label, string? Value, string Table, string Column)[] checks =
        [
            ("Departman", refs.DepartmentSiberId, "sbr_departman", "departmanid"),
            ("Müşteri", refs.CustomerSiberId, "sbr_firma", "firmaid"),
            ("Gönderici", refs.SenderSiberId, "sbr_firma", "firmaid"),
            ("Alıcı", refs.ReceiverSiberId, "sbr_firma", "firmaid"),
            ("Navlun Ödeyecek Firma", refs.CompanyPayFreightSiberId, "sbr_firma", "firmaid"),
        ];

        foreach (var (label, value, table, column) in checks)
        {
            if (string.IsNullOrWhiteSpace(value))
                continue;

            if (!await _siber.ReferenceExistsAsync(table, column, value, cancellationToken))
                return $"\"{label}\" alanındaki kayıt Siber'de bulunamadı — " +
                       "lütfen bu alanı Siber'de tanımlı bir değerle yeniden seçin.";
        }

        return null;
    }

    /// <summary>
    /// Teklifin mali kalemlerinin Siber'de GERÇEKTEN var olduğunu doğrular.
    ///
    /// BULUNAN GERÇEK HATA: yerel <c>financial_items</c> tablosunda DbSeeder'dan
    /// gelen üç kayıt var (Navlun / Gümrükleme / Sigorta) ve bunların siber_id'si
    /// GUID BİÇİMİNDE ama sahte ("bbbb0000-0000-0000-0000-00000000000X") — Siber'in
    /// <c>skn_kalem</c> tablosunda karşılıkları yok. Böyle bir kalem seçilip
    /// "Siber'e Aktar" denince INSERT, FK_skn_rezervasyontarife_skn_kalem kısıtına
    /// takılıyor ve kullanıcıya "beklenmeyen hata" olarak yansıyan işlenmemiş bir
    /// istisnaya dönüşüyordu. Artık önden kontrol edilip HANGİ kalemin geçersiz
    /// olduğunu söyleyen anlaşılır bir mesaj dönüyor.
    /// </summary>
    private async Task<string?> ValidateFinancialItemsExistInSiberAsync(
        DataAccess.Entities.Load load, CancellationToken cancellationToken)
    {
        var itemIds = await _db.LoadFinancialItems
            .Where(f => f.LoadId == load.Id && f.Item != null)
            .Select(f => f.Item!.Value).Distinct().ToListAsync(cancellationToken);

        foreach (var itemId in itemIds)
        {
            var item = await _db.FinancialItems.AsNoTracking()
                .Where(f => f.Id == itemId)
                .Select(f => new { f.Name, f.SiberId }).FirstOrDefaultAsync(cancellationToken);

            if (item is null)
                continue;

            if (string.IsNullOrWhiteSpace(item.SiberId) ||
                !await _siber.KalemExistsAsync(item.SiberId, cancellationToken))
            {
                return $"\"{item.Name}\" mali kalemi Siber'de tanımlı değil — " +
                       "Finans sekmesinden Siber'de var olan bir kalem seçin.";
            }
        }

        return null;
    }

    private static string? ValidateRequired(
        DataAccess.Entities.Load load, OfferRefs r, bool hasContents, bool hasFinancialItems)
    {
        if (r.InstructionCode is null) return "Talimat gelme şekli boş olamaz";
        if (r.RomorkTypeCode is null) return "İstenen Romörk Cinsi boş olamaz";
        if (r.WorkTypeCode is null) return "İş Türü boş olamaz";
        if (r.LoadingTypeCode is null) return "Yükleme Tipi boş olamaz";
        if (r.LoadTransferTypeCode is null) return "Yüktür kodu boş olamaz";
        if (load.MarketingNotificationDate is null) return "Pazarlama bildirim tarihi boş olamaz";
        if (load.OfferDate is null) return "Talimat gelis tarihi boş olamaz";
        if (load.OfferValidityDate is null) return "Geçerlilik tarihi boş olamaz";
        if (r.PaymentTypeSiberId is null) return "Ödeme şekli boş olamaz";
        if (r.CustomerSiberId is null) return "Müşteri boş olamaz";
        if (r.SenderSiberId is null) return "Gönderici boş olamaz";
        if (r.ReceiverSiberId is null) return "Alıcı boş olamaz";
        if (r.StatusTypeSiberId is null) return "Durum boş olamaz";
        if (r.CustomerRepName is null) return "Müşteri temsilcisi boş olamaz";
        if (r.CustomerRepCode is null) return "Müşteri temsilcisi kodu boş olamaz";
        if (r.SalesRepCode is null) return "Satış temsilcisi kodu boş olamaz";
        if (r.DepartmentSiberId is null) return "Departman boş olamaz";
        if (load.DepartureCountryId is null) return "Yükleme ülke boş olamaz";
        if (load.TargetCountryId is null) return "Varış ülke boş olamaz";
        if (!hasContents) return "Yük içerikleri boş olamaz";
        if (!hasFinancialItems) return "Yük finansal kalemleri boş olamaz";

        return null;
    }

    private static DateTime? ToDateTime(DateOnly? date) => date?.ToDateTime(TimeOnly.MinValue);
}
