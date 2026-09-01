using Microsoft.EntityFrameworkCore;
using OLS.Business.Common;
using OLS.Business.Services.Authorization;
using OLS.DataAccess.Context;
using OLS.DataAccess.Entities;
using OLS.DataAccess.Siber;

namespace OLS.Business.Services.LoadTransfers;

/// <summary>
/// TEKLİFSİZ YÜK AÇMA.
///
/// Normal akışta yük, onaylanmış bir teklifin dönüştürülmesiyle doğar
/// (<see cref="ILoadTransferWriteService.ConvertOfferAsync"/>). Avrora tarafında
/// ve yönetimde teklif aşaması olmadan doğrudan yük açılması isteniyor.
///
/// ARKADA GİZLİ TEKLİF AÇILMAZ: teklif akışını çağırıp sonra dönüştürmek
/// kolay olurdu ama Siber'in teklif listesinde gerçek olmayan bir rezervasyon
/// belirirdi. Bunun yerine skn_yuk'a doğrudan yazılır ve rezervasyonid boş
/// bırakılır — Siber bunu zaten destekliyor: canlıda 7.943 yükün 4.270'inde
/// rezervasyonid boş.
///
/// ŞİRKET: yük, açan kullanıcının görme kapsamına yazılır. Avrora kullanıcısı
/// Avrora yükü açar; kapsamı olmayan (OLS) kullanıcı ve yönetici OLS yükü açar.
/// Aksi hâlde Avrora'nın açtığı yük OLS'e düşer ve kendi listesinde görünmezdi.
/// </summary>
public interface IDirectLoadService
{
    Task<LoadTransferWriteResult> CreateAsync(
        DirectLoadModel model, long currentUserId, CancellationToken cancellationToken = default);

    /// <summary>Kullanıcı teklifsiz yük açabilir mi (Avrora ekibi ya da yönetici).</summary>
    Task<bool> CanCreateAsync(long? userId, CancellationToken cancellationToken = default);
}

public sealed class DirectLoadModel
{
    public long? WorkTypeId { get; init; }
    public long? LoadingTypeId { get; init; }
    public long? LoadTransferTypeId { get; init; }
    public long? InstructionId { get; init; }
    public long? RomorkTypeId { get; init; }
    public long? PaymentTypeId { get; init; }
    public long? CustomerId { get; init; }
    public long? SenderId { get; init; }
    public long? ReceiverId { get; init; }
    public long? DepartmentId { get; init; }
    public long? DeliveryMethodId { get; init; }

    /// <summary>Acente ve navlunu ödeyecek firma — teklif formundaki karşılıkları.</summary>
    public long? AgentId { get; init; }
    public long? CompanyPayFreightId { get; init; }
    public string? PayerCompany { get; init; }

    public Guid? DepartureCountryId { get; init; }
    public Guid? TransitCountryId { get; init; }
    public Guid? TargetCountryId { get; init; }

    /// <summary>Ön/son taşıma bizde mi (0/1) ve çalışma şekli — teklifle aynı.</summary>
    public int FrontTransportationByUs { get; init; }
    public int FinalTransportationByUs { get; init; }
    public int WayOfWorking { get; init; }

    public DateOnly? InstructionArrivalDate { get; init; }
    public DateOnly? RequestArrivalDate { get; init; }
    public DateOnly? ReadinessDate { get; init; }

    public string? Description { get; init; }
    public IReadOnlyList<DirectLoadPackage> Packages { get; init; } = [];

    /// <summary>
    /// Mali kalemler. Teklif akışında her kalem Siber'de İKİ satır üretir
    /// (alış = C, satış = G); burada da aynı kural uygulanır ki finans raporları
    /// teklifsiz yüklerde farklı davranmasın.
    /// </summary>
    public IReadOnlyList<DirectLoadFinancialItem> FinancialItems { get; init; } = [];
}

public sealed record DirectLoadFinancialItem(
    long? ItemId, long? AccountId, long? CurrencyId,
    decimal? NetPrice, decimal? Quantity, string? Description);

public sealed record DirectLoadPackage(
    long? ProductTypeId, long? CaseTypeId, int? Quantity,
    decimal? GrossWeight, decimal? NetWeight, decimal? Volume, decimal? Lademeter,
    decimal? Width, decimal? Height, decimal? Length, int? Stackable);

public sealed class DirectLoadService : IDirectLoadService
{
    private readonly OlsDbContext _db;
    private readonly ISiberLoadRepository _siber;
    private readonly ICompanyScope _companyScope;
    private readonly IPermissionService _permissions;
    private readonly IClock _clock;

    public DirectLoadService(
        OlsDbContext db, ISiberLoadRepository siber, ICompanyScope companyScope,
        IPermissionService permissions, IClock clock)
    {
        _db = db;
        _siber = siber;
        _companyScope = companyScope;
        _permissions = permissions;
        _clock = clock;
    }

    public async Task<bool> CanCreateAsync(
        long? userId, CancellationToken cancellationToken = default)
    {
        if (userId is not { } id)
            return false;

        if (await _permissions.HasPermissionAsync(id, "super_admin", PermissionAction.Read, cancellationToken))
            return true;

        // Avrora ekibi: kapsamı Avrora'ya çözülen kullanıcı.
        var visibility = await _companyScope.ResolveAsync(id, cancellationToken);
        return string.Equals(visibility.OnlyCompanyId, CompanyScope.AvroraCompanyId,
            StringComparison.OrdinalIgnoreCase);
    }

    public async Task<LoadTransferWriteResult> CreateAsync(
        DirectLoadModel model, long currentUserId, CancellationToken cancellationToken = default)
    {
        if (!_siber.IsConfigured)
            return LoadTransferWriteResult.Fail("Siber bağlantısı yapılandırılmamış.");

        if (!await CanCreateAsync(currentUserId, cancellationToken))
            return LoadTransferWriteResult.Fail("Teklifsiz yük açma yetkiniz yok.");

        var workType = await _db.WorkTypes.AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == model.WorkTypeId, cancellationToken);
        if (workType?.Code is null)
            return LoadTransferWriteResult.Fail("İş türü boş olamaz");

        var loadingType = await _db.LoadingTypes.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == model.LoadingTypeId, cancellationToken);
        if (loadingType?.Code is null)
            return LoadTransferWriteResult.Fail("Yükleme tipi boş olamaz");

        // MALİ KALEMLER SİBER'E YAZILMADAN ÖNCE DOĞRULANIR.
        //
        // Yük Siber'e yazıldıktan sonra kalem yazımı patlarsa Siber'deki yük
        // geri ALINAMIYOR (yerel işlem geri alınsa bile): kullanıcı hata
        // görüyor ama kayıt Siber'de duruyor. Canlıda tam olarak bu oldu —
        // yerel tabloda taklit Siber'den kalmış üç kalem vardı
        // (Navlun/Gümrükleme/Sigorta) ve gerçek Siber'de karşılıkları yoktu;
        // kullanıcı en doğal görünen bu kalemleri seçince
        // FK_sfy_modulkalem_skn_kalem_kalemid hatası alınıyordu.
        var itemFailure = await ValidateFinancialItemsAsync(model, cancellationToken);
        if (itemFailure is not null)
            return itemFailure;

        var customer = await SiberAccountAsync(model.CustomerId, cancellationToken);
        if (customer is null) return LoadTransferWriteResult.Fail("Müşteri boş olamaz");

        var sender = await SiberAccountAsync(model.SenderId, cancellationToken);
        if (sender is null) return LoadTransferWriteResult.Fail("Gönderici boş olamaz");

        var receiver = await SiberAccountAsync(model.ReceiverId, cancellationToken);
        if (receiver is null) return LoadTransferWriteResult.Fail("Alıcı boş olamaz");

        var department = await _db.Departments.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == model.DepartmentId, cancellationToken);
        if (department?.SiberId is null)
            return LoadTransferWriteResult.Fail("Departman boş olamaz");

        if (model.DepartureCountryId is null) return LoadTransferWriteResult.Fail("Yükleme ülkesi boş olamaz");
        if (model.TargetCountryId is null) return LoadTransferWriteResult.Fail("Varış ülkesi boş olamaz");
        if (model.Packages.Count == 0) return LoadTransferWriteResult.Fail("En az bir paket girilmelidir");

        var paymentType = await _db.PaymentTypes.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == model.PaymentTypeId, cancellationToken);
        var instruction = await _db.Instructions.AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == model.InstructionId, cancellationToken);
        var romorkType = await _db.RomorkTypes.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == model.RomorkTypeId, cancellationToken);
        var loadTransferType = await _db.LoadTransferTypes.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == model.LoadTransferTypeId, cancellationToken);

        var userSiberCode = await _db.Users.AsNoTracking()
            .Where(u => u.Id == currentUserId).Select(u => u.SiberCode)
            .FirstOrDefaultAsync(cancellationToken);

        var visibility = await _companyScope.ResolveAsync(currentUserId, cancellationToken);
        var companyId = visibility.OnlyCompanyId ?? SiberLoadRepository.DefaultSirketId;

        var now = _clock.Now;
        var year = now.ToString("yy");

        var totalQuantity = model.Packages.Sum(p => p.Quantity ?? 0);
        var totalGross = model.Packages.Sum(p => p.GrossWeight ?? 0);
        var totalVolume = model.Packages.Sum(p => p.Volume ?? 0);
        var totalLademeter = model.Packages.Sum(p => p.Lademeter ?? 0);

        var yukId = (await _siber.GenerateYukIdAsync(cancellationToken)).ToString();

        // Yerel taraf tek transaction; Siber yazmaları bittikten SONRA commit
        // edilir (aynı gerekçe ConvertOfferAsync'te ayrıntılı yazılı).
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var numberResult = await _siber.InsertYukWithLockedNumberAsync(new SiberYuk
            {
                YukId = yukId,
                SirketId = companyId,
                // Teklifsiz yük: rezervasyon bağı YOK.
                RezervasyonId = null,
                IsTuru = workType.Code,
                YuklemeTip = loadingType.Code,
                FirmaId = customer,
                GondericiId = sender,
                AliciId = receiver,
                OdemeSekliId = paymentType?.SiberId,
                TalimatGelisSekli = instruction?.Code,
                IstenenRomorkCins = romorkType?.Code,
                ToplamAgirlik = totalGross,
                ToplamHacim = totalVolume,
                ToplamLademetre = totalLademeter,
                ToplamKap = totalQuantity,
                UcretAgirlik = totalLademeter * SiberLoadRepository.LademeterMultiplier,
                MusteriTemsilcisiAd = userSiberCode,
                DepartmanId = department.SiberId,
                YukTurKod = loadTransferType?.Code,
                YuklemeUlke = model.DepartureCountryId?.ToString(),
                BosaltmaUlke = model.TargetCountryId?.ToString(),
                CalismaSekli = model.WayOfWorking,
                TalimatGelisTarihi = now,
                KayitGiren = userSiberCode,
                KayitGirisTarih = now,
            }, year, workType.AdditionalCode ?? string.Empty, cancellationToken);

            var transfer = new LoadTransfer
            {
                LoadTransferId = yukId,
                SiberCompanyId = companyId,
                LoadNumber = numberResult.YukNo.ToString(),
                ConnectedLoadNumber = numberResult.YukNo.ToString(),
                LoadNumberWorkType = numberResult.LoadNumberWorkType,
                ConnectedLoadNumberWorkType = numberResult.LoadNumberWorkType,
                WorkType = (int)workType.Id,
                LoadStatusId = 1,
                LoadTypeId = (int?)loadingType.Id,
                CustomerId = (int?)model.CustomerId,
                SenderId = (int?)model.SenderId,
                ReceiverId = (int?)model.ReceiverId,
                PaymentTypeId = (int?)model.PaymentTypeId,
                InstructionId = (int?)model.InstructionId,
                RomorkTypeId = (int?)model.RomorkTypeId,
                LoadTransferTypeId = (int?)model.LoadTransferTypeId,
                DepartmentId = (int?)model.DepartmentId,
                DepartureCountryId = model.DepartureCountryId?.ToString(),
                TargetCountryId = model.TargetCountryId?.ToString(),
                TotalGrossWeight = totalGross,
                TotalVolume = totalVolume,
                TotalLademeter = totalLademeter,
                TotalCap = totalQuantity,
                WeightFee = totalLademeter * SiberLoadRepository.LademeterMultiplier,
                CarHeight = SiberLoadRepository.DefaultCarHeight,
                LoadingContinent = "ASYA",
                UnloadingContinent = "ASYA",
                CustomerRepresentativeName = (int)currentUserId,
                SecondCustomerRepresentativeName = (int)currentUserId,
                UsercodeWithNotification = (int)currentUserId,
                SalesRepCode = (int)currentUserId,
                InTruck = 1,
                InTail = 1,
                CmrWaiting = 1,
                FcrWaiting = 1,
                DeliveryMethodId = (int?)model.DeliveryMethodId,
                WayOfWorking = model.WayOfWorking,
                FrontTransportationByUs = model.FrontTransportationByUs,
                FinalTransportationByUs = model.FinalTransportationByUs,
                InstructionArrivalDate = model.InstructionArrivalDate,
                RequestArrivalDate = model.RequestArrivalDate,
                ReadinessDate = model.ReadinessDate,
                CreatedAt = now,
                UpdatedAt = now,
            };

            _db.LoadTransfers.Add(transfer);

            foreach (var package in model.Packages)
            {
                var koliId = (await _siber.GenerateYukKoliIdAsync(cancellationToken)).ToString();

                var caseTypeCode = package.CaseTypeId is null ? null : await _db.CaseTypes.AsNoTracking()
                    .Where(c => c.Id == package.CaseTypeId).Select(c => c.SiberId)
                    .FirstOrDefaultAsync(cancellationToken);

                var productCode = package.ProductTypeId is null ? null : await _db.ProductTypes.AsNoTracking()
                    .Where(pt => pt.Id == package.ProductTypeId).Select(pt => pt.SiberId)
                    .FirstOrDefaultAsync(cancellationToken);

                await _siber.InsertYukKoliAsync(new SiberYukKoli
                {
                    YukKoliId = koliId,
                    YukId = yukId,
                    KapAdet = package.Quantity,
                    KapId = caseTypeCode,
                    MalCinsId = productCode,
                    En = package.Width,
                    Boy = package.Length,
                    Yukseklik = package.Height,
                    Hacim = package.Volume,
                    BurutAgirlik = package.GrossWeight,
                    NetAgirlik = package.NetWeight,
                    Lademetre = package.Lademeter,
                    Istiflenemez = package.Stackable ?? 0,
                }, cancellationToken);

                _db.LoadTransferPackages.Add(new LoadTransferPackage
                {
                    Yukkoliid = koliId,
                    LoadTransferId = yukId,
                    ProductTypeId = (int?)package.ProductTypeId,
                    CaseTypeId = package.CaseTypeId?.ToString(),
                    Quantity = package.Quantity,
                    Width = package.Width,
                    Height = package.Height,
                    Length = package.Length,
                    Volume = package.Volume,
                    GrossWeight = package.GrossWeight,
                    NetWeight = package.NetWeight,
                    Lademeter = package.Lademeter,
                    Stackable = package.Stackable,
                });
            }

            await WriteFinancialItemsAsync(
                model, numberResult.LoadNumberWorkType, currentUserId, userSiberCode,
                now, cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return LoadTransferWriteResult.Ok(numberResult.LoadNumberWorkType ?? string.Empty);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }


    /// <summary>
    /// Mali kalemleri hem yerele hem Siber'e yazar.
    ///
    /// Teklif akışıyla AYNI kural: her kalem Siber'de İKİ satır üretir —
    /// alış (buysell=1, gc='C') ve satış (buysell=2, gc='G'). Tek satır
    /// yazılsaydı teklifsiz yükler finans raporlarında teklifli olanlardan
    /// farklı davranırdı.
    ///
    /// Modül kaydı (sfy_modulkayit) yük numarasına göre bulunur; Siber henüz
    /// oluşturmamışsa kalem yerelde açılır ve bir sonraki güncellemede Siber'e
    /// gider (bkz. LoadTransferUpdateService).
    /// </summary>

    /// <summary>
    /// Seçilen mali kalemlerin Siber'de gerçekten var olduğunu doğrular.
    /// Sorun varsa Siber'e HİÇBİR ŞEY yazılmadan hata döner.
    /// </summary>
    private async Task<LoadTransferWriteResult?> ValidateFinancialItemsAsync(
        DirectLoadModel model, CancellationToken cancellationToken)
    {
        var itemIds = model.FinancialItems
            .Where(i => i.ItemId is not null)
            .Select(i => i.ItemId!.Value)
            .Distinct()
            .ToList();

        if (itemIds.Count == 0)
            return null;

        var items = await _db.FinancialItems.AsNoTracking()
            .Where(i => itemIds.Contains(i.Id))
            .Select(i => new { i.Id, i.Name, i.SiberId })
            .ToListAsync(cancellationToken);

        var withoutSiberId = items
            .Where(i => string.IsNullOrWhiteSpace(i.SiberId))
            .Select(i => i.Name ?? i.Id.ToString())
            .ToList();

        if (withoutSiberId.Count > 0)
            return LoadTransferWriteResult.Fail(
                $"Şu mali kalemler Siber'de tanımlı değil: {string.Join(", ", withoutSiberId)}");

        var missingInSiber = await _siber.FindMissingKalemIdsAsync(
            items.Select(i => i.SiberId!).ToList(), cancellationToken);

        if (missingInSiber.Count == 0)
            return null;

        var names = items
            .Where(i => missingInSiber.Contains(i.SiberId!, StringComparer.OrdinalIgnoreCase))
            .Select(i => i.Name ?? i.Id.ToString())
            .ToList();

        return LoadTransferWriteResult.Fail(
            $"Şu mali kalemler Siber'de bulunamadı: {string.Join(", ", names)}. " +
            "Kalem Siber'den silinmiş olabilir; listeyi yenileyip yeniden seçin.");
    }

    private async Task WriteFinancialItemsAsync(
        DirectLoadModel model, string? loadNumberWorkType, long currentUserId,
        string? userSiberCode, DateTime now, CancellationToken cancellationToken)
    {
        if (model.FinancialItems.Count == 0 || loadNumberWorkType is null)
            return;

        var modulKayit = await _siber.FindModulKayitAsync(loadNumberWorkType, cancellationToken);

        foreach (var item in model.FinancialItems)
        {
            var accountSiberId = await SiberAccountAsync(item.AccountId, cancellationToken);

            var currencyCode = item.CurrencyId is null ? null : await _db.Currencies.AsNoTracking()
                .Where(c => c.Id == item.CurrencyId).Select(c => c.Code)
                .FirstOrDefaultAsync(cancellationToken);

            var itemSiberId = item.ItemId is null ? null : await _db.FinancialItems.AsNoTracking()
                .Where(f => f.Id == item.ItemId).Select(f => f.SiberId)
                .FirstOrDefaultAsync(cancellationToken);

            var total = (item.NetPrice ?? 0) * (item.Quantity ?? 0);

            foreach (var buysell in new[] { 1, 2 })
            {
                string? modulKalemId = null;

                if (modulKayit is not null)
                {
                    modulKalemId = (await _siber.GenerateModulKalemIdAsync(cancellationToken)).ToString();

                    await _siber.InsertModulKalemAsync(new SiberModulKalem
                    {
                        ModulKalemId = modulKalemId,
                        ModulId = modulKayit.ModulId,
                        ModulKod = modulKayit.ModulKod,
                        KalemId = itemSiberId,
                        Gc = buysell == 1 ? "C" : "G",
                        FirmaId = accountSiberId,
                        ToplamTutar = total,
                        DovizKod = currencyCode,
                        BirimFiyat = item.NetPrice,
                        Miktar = item.Quantity,
                        Tutar = total,
                        KayitGirisTarih = now,
                        KayitGiren = userSiberCode,
                    }, cancellationToken);
                }

                _db.LoadTransferInvoiceItems.Add(new LoadTransferInvoiceItem
                {
                    Modulkalemid = modulKalemId,
                    Modulid = modulKayit?.ModulId,
                    Modulkod = modulKayit?.ModulKod,
                    ItemId = (int?)item.ItemId,
                    Buysell = buysell.ToString(),
                    AccountId = (int?)item.AccountId,
                    TotalPrice = total,
                    CurrencyCode = (int?)item.CurrencyId,
                    NetPrice = item.NetPrice,
                    Quantity = item.Quantity,
                    TaxPrice = 0,
                    TaxRate = 0,
                    InsertName = loadNumberWorkType,
                    UserId = (int)currentUserId,
                    TransferredFromReservation = 0,
                    Description = item.Description,
                    Status = "pending",
                    CreatedAt = now,
                    UpdatedAt = now,
                });
            }
        }
    }

    private async Task<string?> SiberAccountAsync(long? accountId, CancellationToken cancellationToken) =>
        accountId is null
            ? null
            : await _db.Accounts.AsNoTracking()
                .Where(a => a.Id == accountId)
                .Select(a => a.SiberId)
                .FirstOrDefaultAsync(cancellationToken);
}
