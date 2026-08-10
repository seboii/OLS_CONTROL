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
    private readonly IClock _clock;

    public LoadTransferWriteService(
        OlsDbContext db, ISiberLoadRepository siber, IClock clock)
    {
        _db = db;
        _siber = siber;
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

        if (!MatchesReservation(load, context, reservation))
            return LoadTransferWriteResult.Fail(
                "Verileri Siberle eşleşmiyor lütfen önce sibere aktarın");

        // --- 4) Yük numarası --------------------------------------------------
        var now = _clock.Now;
        var year = now.ToString("yy");

        var yukNo = await _siber.NextYukNoAsync(context.WorkType!.Code, year, cancellationToken);

        // Format: yy + 5 haneli sıfır dolgulu numara + iş türü ek kodu
        var loadNumberWorkType =
            $"{year}{yukNo.ToString().PadLeft(5, '0')}{context.WorkType.AdditionalCode}";

        // --- 5) Toplamlar ------------------------------------------------------
        var contents = await _db.LoadContents.AsNoTracking()
            .Where(c => c.LoadId == load.Id)
            .ToListAsync(cancellationToken);

        var totalVolume = contents.Sum(c => c.Volume ?? 0);
        var totalGrossWeight = contents.Sum(c => c.GrossWeight ?? 0);
        var totalLademeter = contents.Sum(c => c.Lademeter ?? 0);
        var totalQuantity = contents.Sum(c => c.Quantity ?? 0);

        var yukId = (await _siber.GenerateYukIdAsync(cancellationToken)).ToString();

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
        await _db.SaveChangesAsync(cancellationToken);

        await _siber.InsertYukAsync(new SiberYuk
        {
            YukId = yukId,
            YukNo = yukNo,
            IsTuru = context.WorkType.Code,
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
            YukNoIsTuru = loadNumberWorkType,
            ToplamKap = totalQuantity,
            KayitGiren = context.CurrentUserSiberCode,
            Yil = year,
            TalimatGelisTarihi = load.OfferDate?.ToDateTime(TimeOnly.MinValue) ?? now,
            YukTurKod = context.LoadTransferType?.Code,
            YuklemeUlke = load.DepartureCountryId?.ToString(),
            BosaltmaUlke = load.TargetCountryId?.ToString(),
            CalismaSekli = load.WayOfWorking,
            KayitGirisTarih = now,
        }, cancellationToken);

        await WritePackagesAsync(contents, yukId, cancellationToken);
        await WriteInvoiceItemsAsync(
            load, loadNumberWorkType, currentUserId, context, now, cancellationToken);

        // --- 6) Teklifi kapat ---------------------------------------------------
        load.LoadNumber = loadNumberWorkType;
        load.UpdatedAt = now;
        await _db.SaveChangesAsync(cancellationToken);

        return LoadTransferWriteResult.Ok(loadNumberWorkType);
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

        await _db.SaveChangesAsync(cancellationToken);
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

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        IReadOnlyList<long> ids, CancellationToken cancellationToken = default)
    {
        var transfers = await _db.LoadTransfers
            .Where(t => ids.Contains(t.Id)).ToListAsync(cancellationToken);

        _db.LoadTransfers.RemoveRange(transfers);
        await _db.SaveChangesAsync(cancellationToken);
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
        bool HasContents, bool HasFinancialItems, string? ChargePersonSiberName,
        string? ChargePersonSiberCode, string? SalesRepSiberCode);

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
            chargePeople.ElementAtOrDefault(0)?.SiberName,
            chargePeople.ElementAtOrDefault(0)?.SiberCode,
            chargePeople.ElementAtOrDefault(1)?.SiberCode);
    }

    /// <summary>olsold'daki zorunlu alan listesi; ilk eksik alanın mesajı döner.</summary>
    private static string? ValidateRequired(Load load, OfferContext c)
    {
        if (c.StatusType?.SiberId is null) return "Durum boş olamaz";
        if (c.ChargePersonSiberName is null) return "Müşteri temsilcisi boş olamaz";
        if (c.ChargePersonSiberCode is null) return "Müşteri temsilcisi kodu boş olamaz";
        if (c.SalesRepSiberCode is null) return "Satış temsilcisi kodu boş olamaz";
        if (c.Department?.SiberId is null) return "Departman boş olamaz";
        if (load.DepartureCountryId is null) return "Yükleme ülke boş olamaz";
        if (load.TargetCountryId is null) return "Varış ülke boş olamaz";
        if (!c.HasContents) return "Yük içerikleri boş olamaz";
        if (!c.HasFinancialItems) return "Yük finansal kalemleri boş olamaz";
        if (load.WayOfWorking == 0) return "Çalışma şekli boş olamaz";

        return null;
    }

    /// <summary>
    /// Teklifin yerel verisi Siber'deki rezervasyonla tutarlı mı?
    /// olsold dokuz alanı büyük harfe çevirerek karşılaştırıyordu.
    /// </summary>
    private static bool MatchesReservation(
        Load load, OfferContext c, SiberRezervasyon? reservation)
    {
        if (reservation is null)
            return false;

        return Same(load.SiberId, reservation.RezervasyonId)
            && Same(c.RomorkType?.Code, reservation.IstenenRomorkCins)
            && Same(c.WorkType?.Code, reservation.IsTuru)
            && Same(c.Customer?.SiberId, reservation.MusteriId)
            && Same(c.Sender?.SiberId, reservation.GondericiId)
            && Same(c.Receiver?.SiberId, reservation.AliciId)
            && Same(c.PaymentType?.SiberId, reservation.OdemeSekliId)
            && Same(c.StatusType?.SiberId, reservation.DurumId)
            && Same(c.Department?.SiberId, reservation.DepartmanId);
    }

    private static bool Same(string? local, string? siber) =>
        string.Equals(local?.Trim(), siber?.Trim(), StringComparison.OrdinalIgnoreCase);
}
