using Microsoft.EntityFrameworkCore;
using OLS.Business.Common;
using OLS.DataAccess.Context;
using OLS.DataAccess.Entities;
using OLS.DataAccess.Siber;

namespace OLS.Business.Services.LoadTransfers;

/// <summary>
/// Yük Aktarma yazma tarafı. olsold: <c>LoadTransferController::save/update/delete</c>
///
/// DİKKAT — <c>save</c> düz bir "kayıt oluştur" değildir: onaylanmış bir TEKLİFİ
/// (<c>loads</c>) YÜKE (<c>load_transfers</c>) dönüştürür. Akış:
///
///   1. Teklif Siber kimliğiyle bulunur, dört yerel kural kontrol edilir
///      (yük zaten oluşmuş mu, durum "Olumlu" mu, Siber'e aktarılmış mı).
///   2. Zorunlu alanlar tek tek doğrulanır (temsilci, departman, ülkeler, içerik…).
///   3. Siber'deki <c>skn_rezervasyon</c> kaydıyla karşılaştırılır; uyuşmazsa
///      "önce sibere aktarın" hatası döner.
///   4. Yük numarası Siber'den alınır (yıl + iş türü için max + 1).
///   5. load_transfers + skn_yuk, koliler + skn_yukkoli, finansal kalemler +
///      sfy_modulkalem yazılır. Her finansal kalem İKİ satır üretir:
///      alış (buysell=1, GC='C') ve satış (buysell=2).
///   6. Teklifin <c>load_number</c> alanı doldurularak kapatılır.
/// </summary>
public interface ILoadTransferWriteService
{
    /// <summary>Teklifi yüke dönüştürür. <paramref name="loadSiberId"/> teklifin siber_id'si.</summary>
    Task<LoadTransferWriteResult> ConvertOfferAsync(
        string loadSiberId, long currentUserId, CancellationToken cancellationToken = default);

    Task DeleteAsync(IReadOnlyList<long> ids, CancellationToken cancellationToken = default);

    Task DeletePackagesAsync(IReadOnlyList<long> ids, CancellationToken cancellationToken = default);

    Task DeleteInvoiceItemsAsync(IReadOnlyList<long> ids, CancellationToken cancellationToken = default);
}

public sealed record LoadTransferWriteResult(string? LoadNumber, string? ErrorMessage)
{
    public bool IsSuccess => ErrorMessage is null;
    public static LoadTransferWriteResult Fail(string message) => new(null, message);
    public static LoadTransferWriteResult Ok(string loadNumber) => new(loadNumber, null);
}

public sealed class LoadTransferWriteService : ILoadTransferWriteService
{
    /// <summary>olsold: teklif durumu 5 ("Olumlu") olmalı.</summary>
    private const int PositiveStatusTypeId = 5;

    private readonly OlsDbContext _db;
    private readonly ISiberLoadRepository _siber;
    private readonly ISiberReservationRepository _reservations;
    private readonly IClock _clock;

    public LoadTransferWriteService(
        OlsDbContext db, ISiberLoadRepository siber,
        ISiberReservationRepository reservations, IClock clock)
    {
        _db = db;
        _siber = siber;
        _reservations = reservations;
        _clock = clock;
    }

    public async Task<LoadTransferWriteResult> ConvertOfferAsync(
        string loadSiberId, long currentUserId, CancellationToken cancellationToken = default)
    {
        if (!_siber.IsConfigured)
            return LoadTransferWriteResult.Fail("Siber bağlantısı yapılandırılmamış.");

        var load = await _db.Loads
            .FirstOrDefaultAsync(l => l.SiberId == loadSiberId, cancellationToken);

        if (load is null)
            return LoadTransferWriteResult.Fail("Teklif bulunamadı");

        // --- 1) Yerel kurallar (mesajlar olsold'dan birebir) ------------------
        if (load.LoadNumber is not null)
            return LoadTransferWriteResult.Fail("Bu yük zaten oluşturuldu");

        if (load.StatusTypeId != PositiveStatusTypeId)
            return LoadTransferWriteResult.Fail("Yük durumu Olumlu değil");

        if (load.TransferToSiber == 0 || load.SiberId is null)
            return LoadTransferWriteResult.Fail("Önce Teklif Oluşturun");

        var context = await LoadContextAsync(load, cancellationToken);

        // --- 2) Zorunlu alanlar ----------------------------------------------
        if (ValidateRequired(load, context) is { } missing)
            return LoadTransferWriteResult.Fail(missing);

        // --- 3) Siber rezervasyonuyla karşılaştırma ---------------------------
        var reservation = await _siber.FindRezervasyonAsync(load.SiberId, cancellationToken);

        // MÜKERRER YÜK KORUMASI: yukarıdaki yerel "load.LoadNumber is not null"
        // kontrolü, yük DOĞRUDAN Siber ekranından açıldıysa yetmez — o durumda
        // yerel teklifin load_number'ı boş kalır. Canlıda doğrulandı: 25 teklif
        // tam olarak bu hâldeydi. Siber'in kendi bağını (skn_rezervasyon.yukid)
        // sorarak ikinci bir yük açılmasını engelliyoruz.
        if (!string.IsNullOrWhiteSpace(reservation?.YukId))
            return LoadTransferWriteResult.Fail("Bu teklifin yükü Siber'de zaten oluşturulmuş");

        if (!MatchesReservation(load, context, reservation))
            return LoadTransferWriteResult.Fail(
                "Verileri Siberle eşleşmiyor lütfen önce sibere aktarın");

        // --- 4) Toplamlar --------------------------------------------------------
        var now = _clock.Now;
        var year = now.ToString("yy");

        var contents = await _db.LoadContents.AsNoTracking()
            .Where(c => c.LoadId == load.Id)
            .ToListAsync(cancellationToken);

        var totalVolume = contents.Sum(c => c.Volume ?? 0);
        var totalGrossWeight = contents.Sum(c => c.GrossWeight ?? 0);
        var totalLademeter = contents.Sum(c => c.Lademeter ?? 0);
        var totalQuantity = contents.Sum(c => c.Quantity ?? 0);

        var yukId = (await _siber.GenerateYukIdAsync(cancellationToken)).ToString();

        // PostgreSQL↔Siber arası gerçek dağıtık transaction (2PC) yok — iki farklı
        // veritabanı motoru arasında pratikte kurulamaz. Bunun yerine YEREL taraf tek
        // bir transaction'a alınır ve yalnızca TÜM Siber yazmaları (yük, koliler, mali
        // kalemler, teklif↔yük bağlantısı) bittikten SONRA commit edilir: herhangi bir
        // adım hata verirse yerel taraf TAMAMEN geri alınır — yarım kalmış bir Yük
        // kaydı asla görünmez. Siber'de en kötü ihtimalle yetim satırlar kalabilir
        // (zararsız, sonraki deneme yeni yukId/yukno ile temiz başlar), ama yerel taraf
        // ASLA var olmayan bir Siber kaydına işaret eden tutarsız duruma düşmez.
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
        // --- 5) Yük numarası + Siber skn_yuk INSERT'i (atomik, kilitli) ----------
        // Numara PostgreSQL yazımından ÖNCE, Siber ile AYNI çağrıda üretilir — bkz.
        // InsertYukWithLockedNumberAsync'in XML açıklaması (Siber Entegrasyon Raporu
        // risk #3: kilitsiz MAX+1 yarış durumu).
        var numberResult = await _siber.InsertYukWithLockedNumberAsync(new SiberYuk
        {
            YukId = yukId,
            // TERS bağ: Siber'in rezervasyon ekranı bağlı yükü skn_yuk.rezervasyonid
            // üzerinden gösteriyor. İleri yön (skn_rezervasyon.yukid) aşağıda
            // LinkRezervasyonToYukAsync ile yazılıyor; ikisi birlikte gerekli.
            RezervasyonId = load.SiberId,
            IsTuru = context.WorkType!.Code,
            YuklemeTip = context.LoadingType?.Code,
            FirmaId = context.Customer?.SiberId,
            GondericiId = context.Sender?.SiberId,
            AliciId = context.Receiver?.SiberId,
            OdemeSekliId = context.PaymentType?.SiberId,
            TalimatGelisSekli = context.Instruction?.Code,
            IstenenRomorkCins = context.RomorkType?.Code,
            ToplamAgirlik = totalGrossWeight,
            ToplamHacim = totalVolume,
            ToplamLademetre = totalLademeter,
            UcretAgirlik = totalLademeter * SiberLoadRepository.LademeterMultiplier,
            MusteriTemsilcisiAd = context.CurrentUserSiberName,
            DepartmanId = context.Department?.SiberId,
            ToplamKap = totalQuantity,
            KayitGiren = context.CurrentUserSiberCode,
            TalimatGelisTarihi = load.OfferDate?.ToDateTime(TimeOnly.MinValue) ?? now,
            YukTurKod = context.LoadTransferType?.Code,
            YuklemeUlke = load.DepartureCountryId?.ToString(),
            BosaltmaUlke = load.TargetCountryId?.ToString(),
            CalismaSekli = load.WayOfWorking,
            KayitGirisTarih = now,
        }, year, context.WorkType.AdditionalCode ?? string.Empty, cancellationToken);

        var yukNo = numberResult.YukNo;
        var loadNumberWorkType = numberResult.LoadNumberWorkType;

        // İKİNCİ SAVUNMA HATTI — numara yeniden kullanımına karşı.
        //
        // Yük numarası MAX(yukno)+1 ile üretildiği için silinen bir yükün
        // numarası bir sonraki yüke tekrar verilir. Silme artık alt kayıtları
        // temizliyor (bkz. RemoveTransferChildrenAsync), ama Siber ekranından
        // silinmiş ya da bu düzeltmeden ÖNCE oluşmuş yetim kalemler hâlâ
        // durabilir. Yeni yük bunları miras almasın diye numara üretildiği anda
        // aynı numaraya ait eski kalemler temizlenir: bu numara az önce
        // üretildiğine göre buradaki her satır tanımı gereği yetimdir.
        await RemoveOrphanInvoiceItemsAsync(loadNumberWorkType, cancellationToken);

        var transfer = new LoadTransfer
        {
            LoadTransferId = yukId,
            LoadNumber = yukNo.ToString(),
            ConnectedLoadNumber = yukNo.ToString(),
            WorkType = (int)context.WorkType.Id,
            LoadStatusId = 1,
            LoadTypeId = (int?)context.LoadingType?.Id,
            CustomerId = load.CustomerId,
            SenderId = load.SenderId,
            ReceiverId = load.ReceiverId,
            PaymentTypeId = load.PaymentTypeId,
            // olsold bu dört bayrağı yerelde 1, Siber'de 0 yazıyor (kaynakta da öyle).
            InTruck = 1,
            InTail = 1,
            CmrWaiting = 1,
            FcrWaiting = 1,
            InstructionId = load.InstructionId,
            RomorkTypeId = load.RomorkTypeId,
            TotalGrossWeight = totalGrossWeight,
            TotalVolume = totalVolume,
            TotalLademeter = totalLademeter,
            WeightFee = totalLademeter * SiberLoadRepository.LademeterMultiplier,
            CustomerRepresentativeName = (int)currentUserId,
            SecondCustomerRepresentativeName = (int)currentUserId,
            DepartmentId = load.DepartmentId,
            LoadNumberWorkType = loadNumberWorkType,
            ConnectedLoadNumberWorkType = loadNumberWorkType,
            TotalCap = totalQuantity,
            CarHeight = SiberLoadRepository.DefaultCarHeight,
            LoadTransferTypeId = load.LoadTransferTypeId,
            DepartureCountryId = load.DepartureCountryId?.ToString(),
            TargetCountryId = load.TargetCountryId?.ToString(),
            LoadingContinent = "ASYA",
            UnloadingContinent = "ASYA",
            UsercodeWithNotification = (int)currentUserId,
            SalesRepCode = (int)currentUserId,
            WayOfWorking = load.WayOfWorking,
            FrontTransportationByUs = load.FrontTransportationByUs,
            FinalTransportationByUs = load.FinalTransportationByUs,
            // Teklifin siber_id'si: yük hangi rezervasyondan geldi
            SiberId = load.SiberId,
            CreatedAt = now,
            UpdatedAt = now,
        };

        _db.LoadTransfers.Add(transfer);

        // Teklif ↔ yük bağlantısı: Siber'in kendi ekranları teklifin yükünü
        // skn_rezervasyon.yukid üzerinden bulur. Bu adım atlanırsa skn_yuk'ta satır
        // oluşsa bile teklif Siber tarafında yüksüz görünür ve yük numarası teklif
        // üzerinde çıkmaz. Siber Entegrasyon Raporu §6.2 adım 8.
        await _siber.LinkRezervasyonToYukAsync(load.SiberId, yukId, cancellationToken);

        await WritePackagesAsync(contents, yukId, cancellationToken);
        await WriteInvoiceItemsAsync(
            load, loadNumberWorkType, currentUserId, context, now, cancellationToken);

        // --- 6) Teklifi kapat + TEK nihai SaveChanges + commit --------------------
        // Buraya kadar hiçbir SaveChangesAsync çağrılmadı (WritePackagesAsync/
        // WriteInvoiceItemsAsync de kendi SaveChanges'lerini yapmıyor) — bu, tüm
        // yerel değişikliklerin (LoadTransfer + koliler + fatura kalemleri + teklif
        // kapatma) TEK bir veritabanı transaction'ında, tamamı ya da hiçbiri olarak
        // uygulanmasını sağlar.
        load.LoadNumber = loadNumberWorkType;
        load.UpdatedAt = now;
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return LoadTransferWriteResult.Ok(loadNumberWorkType);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task WritePackagesAsync(
        IReadOnlyList<LoadContent> contents, string yukId, CancellationToken cancellationToken)
    {
        foreach (var content in contents)
        {
            var koliId = (await _siber.GenerateYukKoliIdAsync(cancellationToken)).ToString();

            _db.LoadTransferPackages.Add(new LoadTransferPackage
            {
                Yukkoliid = koliId,
                LoadTransferId = yukId,
                Quantity = content.Quantity,
                CaseTypeId = content.CaseTypeId?.ToString(),
                Width = content.Width,
                Length = content.Length,
                Height = content.Height,
                Volume = content.Volume,
                GrossWeight = content.GrossWeight,
                NetWeight = content.NetWeight,
                Lademeter = content.Lademeter,
                Stackable = content.Stackable,
                ProductTypeId = content.ProductTypeId,
                CreatedAt = _clock.Now,
                UpdatedAt = _clock.Now,
            });

            var caseType = content.CaseTypeId is null ? null : await _db.CaseTypes.AsNoTracking()
                .Where(t => t.Id == content.CaseTypeId)
                .Select(t => t.SiberId).FirstOrDefaultAsync(cancellationToken);

            var productType = content.ProductTypeId is null ? null : await _db.ProductTypes.AsNoTracking()
                .Where(t => t.Id == content.ProductTypeId)
                .Select(t => t.SiberId).FirstOrDefaultAsync(cancellationToken);

            await _siber.InsertYukKoliAsync(new SiberYukKoli
            {
                YukKoliId = koliId,
                YukId = yukId,
                KapAdet = content.Quantity,
                KapId = caseType,
                En = content.Width,
                Boy = content.Length,
                Yukseklik = content.Height,
                Hacim = content.Volume,
                BurutAgirlik = content.GrossWeight,
                NetAgirlik = content.NetWeight,
                Lademetre = content.Lademeter,
                Istiflenemez = content.Stackable,
                MalCinsId = productType,
            }, cancellationToken);
        }
        // SaveChanges burada YAPILMAZ — ConvertOfferAsync'in tek, dış transaction'lı
        // final SaveChangesAsync'i bu koli satırlarını da kapsar.
    }

    /// <summary>
    /// Her finansal kalem için İKİ satır üretilir: alış (buysell=1, Siber GC='C')
    /// ve satış (buysell=2). olsold da aynı şekilde çiftliyor.
    /// </summary>
    private async Task WriteInvoiceItemsAsync(
        Load load, string loadNumberWorkType, long currentUserId,
        OfferContext context, DateTime now, CancellationToken cancellationToken)
    {
        var modulKayit = await _siber.FindModulKayitAsync(loadNumberWorkType, cancellationToken);

        var items = await _db.LoadFinancialItems.AsNoTracking()
            .Where(f => f.LoadId == load.Id)
            .ToListAsync(cancellationToken);

        foreach (var item in items)
        {
            var account = item.AccountId is null ? null : await _db.Accounts.AsNoTracking()
                .Where(a => a.Id == item.AccountId)
                .Select(a => new { a.Id, a.SiberId })
                .FirstOrDefaultAsync(cancellationToken);

            var currency = item.Currency is null ? null : await _db.Currencies.AsNoTracking()
                .Where(c => c.Id == item.Currency)
                .Select(c => new { c.Id, c.Code })
                .FirstOrDefaultAsync(cancellationToken);

            var financialItem = item.Item is null ? null : await _db.FinancialItems.AsNoTracking()
                .Where(f => f.Id == item.Item)
                .Select(f => new { f.Id, f.SiberId })
                .FirstOrDefaultAsync(cancellationToken);

            var total = (item.NetPrice ?? 0) * (item.Quantity ?? 0);

            foreach (var buysell in new[] { 1, 2 })
            {
                var modulKalemId = (await _siber.GenerateModulKalemIdAsync(cancellationToken)).ToString();

                _db.LoadTransferInvoiceItems.Add(new LoadTransferInvoiceItem
                {
                    Modulkalemid = modulKalemId,
                    Modulid = modulKayit?.ModulId,
                    Modulkod = modulKayit?.ModulKod,
                    ItemId = (int?)financialItem?.Id,
                    Buysell = buysell.ToString(),
                    AccountId = (int?)account?.Id,
                    TotalPrice = total,
                    CurrencyCode = (int?)currency?.Id,
                    NetPrice = item.NetPrice,
                    Quantity = item.Quantity,
                    TaxPrice = 0,
                    TaxRate = 0,
                    InsertName = loadNumberWorkType,
                    UserId = (int)currentUserId,
                    TransferredFromReservation = 1,
                    Status = "pending",
                    CreatedAt = now,
                    UpdatedAt = now,
                });

                await _siber.InsertModulKalemAsync(new SiberModulKalem
                {
                    ModulKalemId = modulKalemId,
                    ModulId = modulKayit?.ModulId,
                    ModulKod = modulKayit?.ModulKod,
                    KalemId = financialItem?.SiberId,
                    Gc = buysell == 1 ? "C" : "G",
                    FirmaId = account?.SiberId,
                    ToplamTutar = item.TotalPrice,
                    DovizKod = currency?.Code,
                    BirimFiyat = item.NetPrice,
                    Miktar = item.Quantity,
                    Tutar = total,
                    KayitGirisTarih = now,
                    KayitGiren = context.CurrentUserSiberCode,
                }, cancellationToken);
            }
        }
        // SaveChanges burada YAPILMAZ — bkz. WritePackagesAsync'deki aynı not.
    }

    /// <summary>
    /// Yükü siler — ve onu doğuran TEKLİFİ de.
    ///
    /// Kullanıcı isteği: eskiden yalnızca yük siliniyordu, teklif "Olumlu" durumda
    /// ve yük numarası dolu hâlde ortada kalıyordu; raporlamada "olumlu ama yükü
    /// yok" gibi yanlış bir tablo çıkıyordu.
    ///
    /// Siber tarafı da temizlenir (yük + koli + mali kalem + evrak + sefer eşlemesi
    /// ve teklifin rezervasyon kaydı). Bu şart: yalnızca yerelden silinirse
    /// periyodik senkron bir sonraki turda hem yükü hem teklifi Siber'den geri
    /// getirir — silme kalıcı olmaz.
    /// </summary>
    public async Task DeleteAsync(
        IReadOnlyList<long> ids, CancellationToken cancellationToken = default)
    {
        var transfers = await _db.LoadTransfers
            .Where(t => ids.Contains(t.Id)).ToListAsync(cancellationToken);

        if (transfers.Count == 0)
            return;

        // Teklif bağı: dönüşümde load.LoadNumber = yük numarası yazılıyor
        // (bkz. ConvertOfferAsync), Siber kimliği de teklifle aynı.
        var loadNumbers = transfers
            .Select(t => t.LoadNumberWorkType).Where(n => n != null).Cast<string>().ToList();

        var loads = loadNumbers.Count == 0
            ? []
            : await _db.Loads.Where(l => l.LoadNumber != null && loadNumbers.Contains(l.LoadNumber))
                .ToListAsync(cancellationToken);

        if (loads.Count > 0)
        {
            var loadIds = loads.Select(l => l.Id).ToList();
            // LoadChargePerson.LoadId int? (diğer iki alt tabloda long) — ayrı liste.
            var loadIdsInt = loadIds.Select(id => (int)id).ToList();

            // Teklifin alt kayıtları FK ile bağlı değil; elle temizlenmeli.
            _db.LoadContents.RemoveRange(
                await _db.LoadContents.Where(c => loadIds.Contains(c.LoadId)).ToListAsync(cancellationToken));
            _db.LoadFinancialItems.RemoveRange(
                await _db.LoadFinancialItems.Where(f => loadIds.Contains(f.LoadId)).ToListAsync(cancellationToken));
            _db.LoadChargePeople.RemoveRange(
                await _db.LoadChargePeople.Where(p => p.LoadId != null && loadIdsInt.Contains(p.LoadId.Value)).ToListAsync(cancellationToken));

            _db.Loads.RemoveRange(loads);
        }

        // BULUNAN GERÇEK HATA — YÜKÜN FİNANS/KOLİ/EVRAK KAYITLARI SİLİNMİYORDU.
        //
        // load_transfer_invoice_items'ta load_transfer_id sütunu YOKTUR; kalem
        // yüke <c>insert_name = yük numarası</c> METİN eşleşmesiyle bağlanır
        // (Siber'in sfy_modulkayit.ad kuralı, bkz. LoadTransferService.SingleAsync).
        // Yük silinince bu satırlar geride kalıyordu — ve yük numarası sayacı
        // MAX(yukno)+1 olduğu için SİLİNEN NUMARA BİR SONRAKİ YÜKE YENİDEN
        // VERİLİYOR: yeni yük, ölü yükün finans kalemlerini miras alıyor ve
        // kullanıcı hiç girmediği "GÜMRÜKLEME GELİRİ" gibi satırlar görüyordu.
        // Canlıda doğrulandı: 2600838TR'de 10 kalemin 6'sı silinmiş yükten
        // geliyordu, 2600839TR'de ise hiçbir yüke ait olmayan 2 yetim satır vardı.
        await RemoveTransferChildrenAsync(transfers, cancellationToken);

        _db.LoadTransfers.RemoveRange(transfers);

        // SIRA ÖNEMLİ — ÖNCE SİBER, SONRA YEREL.
        //
        // Ters sırada (yerel önce) canlıda şu hataya düşüldü: yerel silme commit
        // edildikten sonra Siber silme bir tetikleyiciye takıldı; yerel kayıt gitti,
        // Siber'deki kaldı ve periyodik senkron kaydı YENİ bir yerel id ile geri
        // getirdi. Kullanıcı "silindi" mesajı aldı ama kayıt duruyordu, üstelik
        // ikinci deneme eski id'yi bulamadığı için sessizce hiçbir şey yapmadı.
        //
        // Siber önce silinirse: Siber başarısız olursa istisna yükselir, yerel
        // SaveChanges hiç çalışmaz ve iki taraf da tutarlı kalır.
        if (_siber.IsConfigured)
        {
            foreach (var transfer in transfers.Where(t => t.LoadTransferId is not null))
                await _siber.DeleteYukAsync(transfer.LoadTransferId!, cancellationToken);

            foreach (var siberId in loads.Select(l => l.SiberId).Where(s => s is not null).Distinct())
                await _reservations.DeleteRezervasyonAsync(siberId!, cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }



    /// <summary>
    /// Verilen yük numarasına ait ARTIK BİR YÜKE AİT OLMAYAN finans kalemlerini
    /// siler. Yalnızca yeni numara üretildiği anda çağrılır — o noktada aynı
    /// numarayla eşleşen her satır, silinmiş bir yükten kalmış demektir.
    /// </summary>
    private async Task RemoveOrphanInvoiceItemsAsync(
        string? loadNumberWorkType, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(loadNumberWorkType))
            return;

        var orphanIds = await _db.LoadTransferInvoiceItems
            .Where(i => i.InsertName == loadNumberWorkType)
            .Select(i => i.Id)
            .ToListAsync(cancellationToken);

        if (orphanIds.Count == 0)
            return;

        _db.LoadTransferInvoiceMaps.RemoveRange(
            await _db.LoadTransferInvoiceMaps
                .Where(m => orphanIds.Contains(m.InvoiceItemId))
                .ToListAsync(cancellationToken));

        _db.LoadTransferInvoiceItems.RemoveRange(
            await _db.LoadTransferInvoiceItems
                .Where(i => orphanIds.Contains(i.Id))
                .ToListAsync(cancellationToken));
    }

    /// <summary>
    /// Bir yükün FK ile bağlı OLMAYAN alt kayıtlarını temizler: finans kalemleri
    /// (+ fatura eşlemeleri), koliler, evraklar ve hareketler.
    ///
    /// Finans kalemleri yüke metin eşleşmesiyle bağlı olduğu için (bkz.
    /// <see cref="DeleteAsync"/>'teki gerekçe) burada YÜK NUMARASI üzerinden
    /// silinir. Bu aynı zamanda yeniden kullanılan numaraların ölü kalem miras
    /// almasını da engeller.
    /// </summary>
    private async Task RemoveTransferChildrenAsync(
        IReadOnlyList<LoadTransfer> transfers, CancellationToken cancellationToken)
    {
        var transferIds = transfers.Select(t => t.Id).ToList();

        var loadNumbers = transfers
            .Select(t => t.LoadNumberWorkType)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Cast<string>()
            .Distinct()
            .ToList();

        var siberYukIds = transfers
            .Select(t => t.LoadTransferId)
            .Where(i => !string.IsNullOrWhiteSpace(i))
            .Cast<string>()
            .Distinct()
            .ToList();

        if (loadNumbers.Count > 0)
        {
            var invoiceItemIds = await _db.LoadTransferInvoiceItems
                .Where(i => i.InsertName != null && loadNumbers.Contains(i.InsertName))
                .Select(i => i.Id)
                .ToListAsync(cancellationToken);

            if (invoiceItemIds.Count > 0)
            {
                // Fatura eşlemeleri önce — kalem satırına FK ile bağlılar.
                _db.LoadTransferInvoiceMaps.RemoveRange(
                    await _db.LoadTransferInvoiceMaps
                        .Where(m => invoiceItemIds.Contains(m.InvoiceItemId))
                        .ToListAsync(cancellationToken));

                _db.LoadTransferInvoiceItems.RemoveRange(
                    await _db.LoadTransferInvoiceItems
                        .Where(i => invoiceItemIds.Contains(i.Id))
                        .ToListAsync(cancellationToken));
            }
        }

        if (siberYukIds.Count > 0)
        {
            // Koliler yüke Siber kimliğiyle (metin sütun) bağlı.
            _db.LoadTransferPackages.RemoveRange(
                await _db.LoadTransferPackages
                    .Where(p => p.LoadTransferId != null && siberYukIds.Contains(p.LoadTransferId))
                    .ToListAsync(cancellationToken));
        }

        _db.LoadTransferDocuments.RemoveRange(
            await _db.LoadTransferDocuments
                .Where(d => transferIds.Contains(d.LoadTransferId))
                .ToListAsync(cancellationToken));

        _db.LoadTransferMovements.RemoveRange(
            await _db.LoadTransferMovements
                .Where(m => m.LoadTransferId != null && transferIds.Contains(m.LoadTransferId.Value))
                .ToListAsync(cancellationToken));
    }

    /// <summary>
    /// olsold: koli silinirken önce Siber'den, sonra yerelden siliniyordu.
    /// Sırayı tersine çevirdik — yerel silme başarısız olursa Siber'de kayıp
    /// oluşmasın (kaynak sırada Siber'den silinip yerel hata verirse veri kaçardı).
    /// </summary>
    public async Task DeletePackagesAsync(
        IReadOnlyList<long> ids, CancellationToken cancellationToken = default)
    {
        var packages = await _db.LoadTransferPackages
            .Where(p => ids.Contains(p.Id)).ToListAsync(cancellationToken);

        _db.LoadTransferPackages.RemoveRange(packages);
        await _db.SaveChangesAsync(cancellationToken);

        if (!_siber.IsConfigured)
            return;

        foreach (var package in packages.Where(p => p.Yukkoliid is not null))
            await _siber.DeleteYukKoliAsync(package.Yukkoliid!, cancellationToken);
    }

    public async Task DeleteInvoiceItemsAsync(
        IReadOnlyList<long> ids, CancellationToken cancellationToken = default)
    {
        var items = await _db.LoadTransferInvoiceItems
            .Where(i => ids.Contains(i.Id)).ToListAsync(cancellationToken);

        _db.LoadTransferInvoiceItems.RemoveRange(items);
        await _db.SaveChangesAsync(cancellationToken);

        if (!_siber.IsConfigured)
            return;

        foreach (var item in items.Where(i => i.Modulkalemid is not null))
            await _siber.DeleteModulKalemAsync(item.Modulkalemid!, cancellationToken);
    }

    /// <summary>Teklif dönüşümünde gereken tüm ilişkili kayıtlar.</summary>
    private sealed record OfferContext(
        WorkType? WorkType, LoadingType? LoadingType, Account? Customer, Account? Sender,
        Account? Receiver, PaymentType? PaymentType, Instruction? Instruction,
        RomorkType? RomorkType, Department? Department, LoadTransferType? LoadTransferType,
        StatusType? StatusType, string? CurrentUserSiberName, string? CurrentUserSiberCode,
        bool HasContents, bool HasFinancialItems, bool HasFinancialItemWithoutKalem,
        string? ChargePersonSiberName, string? ChargePersonSiberCode, string? SalesRepSiberCode,
        Account? CompanyPayFreight);

    private async Task<OfferContext> LoadContextAsync(Load load, CancellationToken cancellationToken)
    {
        var chargePeople = await _db.LoadChargePeople.AsNoTracking()
            .Where(p => p.LoadId == (int)load.Id)
            .OrderBy(p => p.Id)
            .Join(_db.Users, p => p.UserId, u => (int)u.Id,
                (p, u) => new { u.SiberName, u.SiberCode })
            .ToListAsync(cancellationToken);

        return new OfferContext(
            await _db.WorkTypes.AsNoTracking().FirstOrDefaultAsync(w => w.Id == load.WorkTypeId, cancellationToken),
            await _db.LoadingTypes.AsNoTracking().FirstOrDefaultAsync(t => t.Id == load.LoadingTypeId, cancellationToken),
            await _db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == load.CustomerId, cancellationToken),
            await _db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == load.SenderId, cancellationToken),
            await _db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == load.ReceiverId, cancellationToken),
            await _db.PaymentTypes.AsNoTracking().FirstOrDefaultAsync(p => p.Id == load.PaymentTypeId, cancellationToken),
            await _db.Instructions.AsNoTracking().FirstOrDefaultAsync(i => i.Id == load.InstructionId, cancellationToken),
            await _db.RomorkTypes.AsNoTracking().FirstOrDefaultAsync(r => r.Id == load.RomorkTypeId, cancellationToken),
            await _db.Departments.AsNoTracking().FirstOrDefaultAsync(d => d.Id == load.DepartmentId, cancellationToken),
            await _db.LoadTransferTypes.AsNoTracking().FirstOrDefaultAsync(t => t.Id == load.LoadTransferTypeId, cancellationToken),
            await _db.StatusTypes.AsNoTracking().FirstOrDefaultAsync(s => s.Id == load.StatusTypeId, cancellationToken),
            // Siber'e "müşteri temsilcisi" ve "kayıt giren" olarak yükün ilk
            // görevlisi yazılır (olsold da loadChargePerson[0]'ı kullanıyordu).
            chargePeople.ElementAtOrDefault(0)?.SiberName,
            chargePeople.ElementAtOrDefault(0)?.SiberCode,
            await _db.LoadContents.AnyAsync(c => c.LoadId == load.Id, cancellationToken),
            await _db.LoadFinancialItems.AnyAsync(f => f.LoadId == load.Id, cancellationToken),
            // sfy_modulkalem.kalemid gerçek Siber'de NOT NULL — Kalem'i boş bir mali
            // kalem varsa dönüşüm burada durmalı, aksi hâlde hata Siber INSERT'inde
            // (WriteInvoiceItemsAsync) yakalanmadan patlar. Bkz. Siber Entegrasyon
            // Raporu; 18 ETL-senkron kaydında Kalem gerçekten boş (kaynakta da NULL,
            // bu bir eşleme hatası değil) — o kayıtlar burada nazikçe reddedilir.
            await _db.LoadFinancialItems.AnyAsync(f => f.LoadId == load.Id && f.Item == null, cancellationToken),
            chargePeople.ElementAtOrDefault(0)?.SiberName,
            chargePeople.ElementAtOrDefault(0)?.SiberCode,
            chargePeople.ElementAtOrDefault(1)?.SiberCode,
            await _db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == load.CompanyPayFreightId, cancellationToken));
    }

    /// <summary>
    /// olsold'daki zorunlu alan listesi ($fields dizisi, LoadTransferController::save);
    /// mesajlar VE SIRA birebir korunur. Önceki sürüm yalnızca son 9 kontrolü
    /// içeriyordu — talimat/römork/iş türü/yükleme tipi/yüktür/tarihler/ödeme
    /// şekli/müşteri/gönderici/alıcı hiç doğrulanmıyordu.
    /// "Çalışma şekli boş olamaz" kaldırıldı: olsold'da <c>way_of_working</c> NOT
    /// NULL + default 0 olduğundan ve kontrol <c>=== null || === ''</c> kullandığından
    /// (0, ikisine de eşit değil) kaynakta da fiilen hiç tetiklenmiyordu — burada 0
    /// ("Spot") geçerli bir seçimken hatalı biçimde reddediliyordu, düzeltildi.
    /// </summary>
    private static string? ValidateRequired(Load load, OfferContext c)
    {
        if (c.Instruction?.Code is null) return "Talimat gelme şekli boş olamaz";
        if (c.RomorkType?.Code is null) return "İstenen Romörk Cinsi boş olamaz";
        if (c.WorkType?.Code is null) return "İş Türü boş olamaz";
        if (c.LoadingType?.Code is null) return "Yükleme Tipi boş olamaz";
        if (c.LoadTransferType?.Code is null) return "Yüktür kodu boş olamaz";
        if (load.MarketingNotificationDate is null) return "Pazarlama bildirim tarihi boş olamaz";
        if (load.OfferDate is null) return "Talimat gelis tarihi boş olamaz";
        if (load.OfferValidityDate is null) return "Geçerlilik tarihi boş olamaz";
        if (c.PaymentType?.SiberId is null) return "Ödeme şekli boş olamaz";
        if (c.Customer?.SiberId is null) return "Müşteri boş olamaz";
        if (c.Sender?.SiberId is null) return "Gönderici boş olamaz";
        if (c.Receiver?.SiberId is null) return "Alıcı boş olamaz";
        if (c.StatusType?.SiberId is null) return "Durum boş olamaz";
        if (c.ChargePersonSiberName is null) return "Müşteri temsilcisi boş olamaz";
        if (c.ChargePersonSiberCode is null) return "Müşteri temsilcisi kodu boş olamaz";
        if (c.SalesRepSiberCode is null) return "Satış temsilcisi kodu boş olamaz";
        if (c.Department?.SiberId is null) return "Departman boş olamaz";
        if (load.DepartureCountryId is null) return "Yükleme ülke boş olamaz";
        if (load.TargetCountryId is null) return "Varış ülke boş olamaz";
        if (!c.HasContents) return "Yük içerikleri boş olamaz";
        if (!c.HasFinancialItems) return "Yük finansal kalemleri boş olamaz";
        if (c.HasFinancialItemWithoutKalem) return "Mali kalemlerden birinde kalem seçilmemiş";

        return null;
    }

    /// <summary>
    /// Teklifin yerel verisi Siber'deki rezervasyonla tutarlı mı?
    /// olsold on sekiz alanı büyük harfe çevirerek karşılaştırıyordu (biri —
    /// departmanid — kaynakta yanlışlıkla iki kez tekrarlanmış, tek kontrol
    /// yeterli). Önceki sürüm yalnızca 9'unu içeriyordu — talimat/yükleme
    /// tipi/yüktür/navlun firma/ülkeler/taşıma bayrakları/çalışma şekli hiç
    /// karşılaştırılmıyordu; Siber'deki veri bu alanlarda uyuşmasa bile
    /// dönüşüm sessizce devam ederdi.
    /// </summary>
    private static bool MatchesReservation(
        Load load, OfferContext c, SiberRezervasyon? reservation)
    {
        if (reservation is null)
            return false;

        return Same(load.SiberId, reservation.RezervasyonId)
            && Same(c.RomorkType?.Code, reservation.IstenenRomorkCins)
            && Same(c.WorkType?.Code, reservation.IsTuru)
            && Same(c.Instruction?.Code, reservation.TalimatGelisSekli)
            && Same(c.LoadingType?.Code, reservation.YuklemeTip)
            && Same(c.LoadTransferType?.Code, reservation.YukTurKod)
            && Same(c.PaymentType?.SiberId, reservation.OdemeSekliId)
            && load.FrontTransportationByUs == (reservation.OnTasimaTarafimizdanYapilir ?? 0)
            && load.FinalTransportationByUs == (reservation.SonTasimaTarafimizdanYapilir ?? 0)
            && Same(c.Customer?.SiberId, reservation.MusteriId)
            && Same(c.CompanyPayFreight?.SiberId, reservation.NavlunFirmaId)
            && Same(c.Sender?.SiberId, reservation.GondericiId)
            && Same(c.Receiver?.SiberId, reservation.AliciId)
            && Same(c.StatusType?.SiberId, reservation.DurumId)
            && Same(c.Department?.SiberId, reservation.DepartmanId)
            && Same(load.DepartureCountryId?.ToString(), reservation.YuklemeUlkeId)
            && Same(load.TargetCountryId?.ToString(), reservation.BosaltmaUlkeId)
            && load.WayOfWorking == (reservation.CalismaSekli ?? 0);
    }

    private static bool Same(string? local, string? siber) =>
        string.Equals(local?.Trim(), siber?.Trim(), StringComparison.OrdinalIgnoreCase);
}
