using Microsoft.EntityFrameworkCore;
using OLS.Business.Common;
using OLS.DataAccess.Context;
using OLS.DataAccess.Entities;
using OLS.DataAccess.Siber;

namespace OLS.Business.Services.LoadTransfers;

/// <summary>
/// Yük aktarma düzenleme (dönüşüm sonrası).
/// olsold: <c>LoadTransferController::update</c> — <c>POST /load_transfer/{id}</c>
///
/// Dönüşümden (<see cref="ILoadTransferWriteService.ConvertOfferAsync"/>) farkı:
/// yük numarası, iş türü, Siber kimlikleri ve yıl <b>değişmez</b>. Kaynak da bu
/// alanları güncelleme listesinden çıkarmış (yorum satırı yapmış) — çünkü yük
/// numarası Siber'de başka kayıtlarla ilişkilendirilmiş durumda.
///
/// Alt kayıtlar (koli / finansal kalem) <b>silinip yeniden yazılmaz</b>:
/// Siber kimliği olan satır güncellenir, olmayan eklenir. Silme ayrı uçlardan
/// yapılır — aksi hâlde her düzenlemede Siber'de yeni kimlikler üretilir ve
/// eski satırlar yetim kalır.
/// </summary>
public interface ILoadTransferUpdateService
{
    Task<LoadTransferWriteResult> UpdateAsync(
        LoadTransferUpdateRequest request, long currentUserId,
        CancellationToken cancellationToken = default);
}

public sealed class LoadTransferUpdateRequest
{
    public long Id { get; set; }

    public int? LoadStatusId { get; set; }
    public int? LoadTypeId { get; set; }
    public int? CustomerId { get; set; }
    public int? SenderId { get; set; }
    public int? ReceiverId { get; set; }
    public int? PaymentTypeId { get; set; }
    public int? InTruck { get; set; }
    public int? InTail { get; set; }
    public int? CmrWaiting { get; set; }
    public int? FcrWaiting { get; set; }
    public int? InstructionId { get; set; }
    public int? RomorkTypeId { get; set; }
    public decimal? TotalGrossWeight { get; set; }
    public decimal? TotalVolume { get; set; }
    public decimal? TotalLademeter { get; set; }
    public decimal? TotalQuantity { get; set; }
    public int? DepartmentId { get; set; }
    public int? LoadTransferTypeId { get; set; }
    public int? DeliveryMethodId { get; set; }

    /// <summary>
    /// olsold "Görevliler" sekmesi: Operasyon Yetkilisi / Satış Temsilcisi.
    /// Sütun adı yanıltıcı (<c>customer_representative_name</c>) ama içeriği
    /// bir kullanıcı kimliğidir — dönüşüm sırasında hep işlemi yapan
    /// kullanıcıya sabitleniyordu, bu uçla artık düzenlenebilir.
    /// </summary>
    public int? CustomerRepresentativeUserId { get; set; }
    public int? SecondCustomerRepresentativeUserId { get; set; }
    public string? DepartureCountryId { get; set; }
    public string? TargetCountryId { get; set; }
    public int? WayOfWorking { get; set; }
    public int? FrontTransportationByUs { get; set; }
    public int? FinalTransportationByUs { get; set; }
    public DateOnly? InstructionArrivalDate { get; set; }
    public DateOnly? RequestArrivalDate { get; set; }
    public DateOnly? ReadinessDate { get; set; }
    public DateOnly? DateOfReceiptCustomer { get; set; }

    public List<PackageInput>? Packages { get; set; }
    public List<InvoiceItemInput>? InvoiceItems { get; set; }

    public sealed class PackageInput
    {
        public long? Id { get; set; }
        public int? ProductTypeId { get; set; }
        public int? CaseTypeId { get; set; }
        public int? Quantity { get; set; }
        public decimal? Width { get; set; }
        public decimal? Height { get; set; }
        public decimal? Length { get; set; }
        public decimal? Volume { get; set; }
        public decimal? GrossWeight { get; set; }
        public decimal? NetWeight { get; set; }
        public decimal? Lademeter { get; set; }
        public int? Stackable { get; set; }
    }

    public sealed class InvoiceItemInput
    {
        public long? Id { get; set; }
        public int? ItemId { get; set; }
        public string? Buysell { get; set; }
        public int? AccountId { get; set; }
        public decimal? Quantity { get; set; }
        public decimal? NetPrice { get; set; }
        public decimal? TotalPrice { get; set; }
        public decimal? TaxPrice { get; set; }
        public decimal? TaxRate { get; set; }
        public int? CurrencyCode { get; set; }
        public string? Description { get; set; }

        /// <summary>olsold: pending / invoice_received / invoice_issued — göndermezse "pending".</summary>
        public string? Status { get; set; }
    }
}

public sealed class LoadTransferUpdateService : ILoadTransferUpdateService
{
    /// <summary>Siber'deki sabit çarpan (olsold: ücret ağırlığı = lademetre × 1750).</summary>
    private const decimal WeightFeeMultiplier = 1750m;

    private readonly OlsDbContext _db;
    private readonly ISiberLoadRepository _siber;
    private readonly IClock _clock;

    public LoadTransferUpdateService(
        OlsDbContext db, ISiberLoadRepository siber, IClock clock)
    {
        _db = db;
        _siber = siber;
        _clock = clock;
    }

    public async Task<LoadTransferWriteResult> UpdateAsync(
        LoadTransferUpdateRequest request, long currentUserId,
        CancellationToken cancellationToken = default)
    {
        var transfer = await _db.LoadTransfers
            .FirstOrDefaultAsync(l => l.Id == request.Id, cancellationToken);

        if (transfer is null)
            return LoadTransferWriteResult.Fail("Yük aktarma kaydı bulunamadı.");

        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == currentUserId, cancellationToken);

        var now = _clock.Now;

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

        ApplyFields(transfer, request, currentUserId, now);

        await _db.SaveChangesAsync(cancellationToken);

        await UpsertPackagesAsync(transfer, request, cancellationToken);
        await UpsertInvoiceItemsAsync(transfer, request, user, now, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);

        // Siber tarafı yalnızca bağlantı yapılandırılmışsa güncellenir; yerel
        // kayıt her hâlükârda kaydedilir (olsold Siber hatasında tamamını geri
        // alıyordu — yerel düzenlemenin kaybolması daha kötü bir sonuç).
        if (_siber.IsConfigured && !string.IsNullOrEmpty(transfer.LoadTransferId))
            await SyncSiberAsync(transfer, user, cancellationToken);

        await tx.CommitAsync(cancellationToken);

        return LoadTransferWriteResult.Ok(transfer.LoadNumberWorkType ?? string.Empty);
    }

    /// <summary>
    /// Yerel alanlar. Yük numarası / iş türü / Siber kimlikleri KASITLI OLARAK
    /// dışarıda — bkz. sınıf açıklaması.
    /// </summary>
    private static void ApplyFields(
        LoadTransfer transfer, LoadTransferUpdateRequest request, long userId, DateTime now)
    {
        transfer.LoadStatusId = request.LoadStatusId;
        transfer.LoadTypeId = request.LoadTypeId;
        transfer.CustomerId = request.CustomerId;
        transfer.SenderId = request.SenderId;
        transfer.ReceiverId = request.ReceiverId;
        transfer.PaymentTypeId = request.PaymentTypeId;
        transfer.InTruck = request.InTruck;
        transfer.InTail = request.InTail;
        transfer.CmrWaiting = request.CmrWaiting;
        transfer.FcrWaiting = request.FcrWaiting;
        transfer.InstructionId = request.InstructionId;
        transfer.RomorkTypeId = request.RomorkTypeId;
        transfer.TotalGrossWeight = request.TotalGrossWeight;
        transfer.TotalVolume = request.TotalVolume;
        transfer.TotalLademeter = request.TotalLademeter;
        transfer.WeightFee = (request.TotalLademeter ?? 0) * WeightFeeMultiplier;
        transfer.DepartmentId = request.DepartmentId;
        transfer.TotalCap = request.TotalQuantity;
        transfer.LoadTransferTypeId = request.LoadTransferTypeId;
        transfer.DeliveryMethodId = request.DeliveryMethodId;
        transfer.CustomerRepresentativeName = request.CustomerRepresentativeUserId;
        transfer.SecondCustomerRepresentativeName = request.SecondCustomerRepresentativeUserId;
        transfer.DepartureCountryId = request.DepartureCountryId;
        transfer.TargetCountryId = request.TargetCountryId;
        transfer.WayOfWorking = request.WayOfWorking;
        transfer.FrontTransportationByUs = request.FrontTransportationByUs;
        transfer.FinalTransportationByUs = request.FinalTransportationByUs;
        transfer.InstructionArrivalDate = request.InstructionArrivalDate;
        transfer.RequestArrivalDate = request.RequestArrivalDate;
        transfer.ReadinessDate = request.ReadinessDate;
        transfer.DateOfReceiptCustomer = request.DateOfReceiptCustomer;

        // Kaynaktaki sabitler.
        transfer.TotalLademeterM3 = 0;
        transfer.CarHeight = 280;
        transfer.LoadingContinent = "ASYA";
        transfer.UnloadingContinent = "ASYA";
        transfer.UsercodeWithNotification = (int)userId;
        transfer.SalesRepCode = (int)userId;
        transfer.UpdatedAt = now;
    }

    private async Task UpsertPackagesAsync(
        LoadTransfer transfer, LoadTransferUpdateRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Packages is not { Count: > 0 })
            return;

        // DİKKAT: load_transfer_packages.load_transfer_id yerel id DEĞİL,
        // Siber'deki yukid'yi (string) tutar — dönüşüm kodu da öyle yazıyor.
        var existing = await _db.LoadTransferPackages
            .Where(p => p.LoadTransferId == transfer.LoadTransferId)
            .ToListAsync(cancellationToken);

        foreach (var input in request.Packages)
        {
            var package = input.Id is { } id
                ? existing.FirstOrDefault(p => p.Id == id)
                : null;

            if (package is null)
            {
                package = new LoadTransferPackage { LoadTransferId = transfer.LoadTransferId };
                _db.LoadTransferPackages.Add(package);
            }

            package.ProductTypeId = input.ProductTypeId;
            package.CaseTypeId = input.CaseTypeId?.ToString();
            package.Quantity = input.Quantity;
            package.Width = input.Width;
            package.Height = input.Height;
            package.Length = input.Length;
            package.Volume = input.Volume;
            package.GrossWeight = input.GrossWeight;
            package.NetWeight = input.NetWeight;
            package.Lademeter = input.Lademeter;
            package.Stackable = input.Stackable;
        }
    }

    private async Task UpsertInvoiceItemsAsync(
        LoadTransfer transfer, LoadTransferUpdateRequest request, User? user,
        DateTime now, CancellationToken cancellationToken)
    {
        if (request.InvoiceItems is not { Count: > 0 })
            return;

        var existing = await _db.LoadTransferInvoiceItems
            .Where(i => i.InsertName == transfer.LoadNumberWorkType)
            .ToListAsync(cancellationToken);

        foreach (var input in request.InvoiceItems)
        {
            var item = input.Id is { } id
                ? existing.FirstOrDefault(i => i.Id == id)
                : null;

            if (item is null)
            {
                item = new LoadTransferInvoiceItem
                {
                    InsertName = transfer.LoadNumberWorkType,
                    UserId = user is null ? null : (int)user.Id,
                    CreatedAt = now,
                };

                _db.LoadTransferInvoiceItems.Add(item);
            }

            item.ItemId = input.ItemId;
            item.Buysell = input.Buysell;
            item.AccountId = input.AccountId;
            item.Quantity = input.Quantity;
            item.NetPrice = input.NetPrice;
            item.TotalPrice = input.TotalPrice;
            item.TaxPrice = input.TaxPrice;
            item.TaxRate = input.TaxRate;
            item.CurrencyCode = input.CurrencyCode;
            item.Description = input.Description;
            // olsold: $item['status'] ?? 'pending' — hem yeni hem mevcut satırda aynı kural.
            item.Status = input.Status ?? "pending";
            item.UpdatedAt = now;
        }
    }

    /// <summary>Yerel kaydı Siber'e yansıtır (skn_yuk + koli + modül kalemleri).</summary>
    private async Task SyncSiberAsync(
        LoadTransfer transfer, User? user, CancellationToken cancellationToken)
    {
        var refs = await LoadSiberRefsAsync(transfer, cancellationToken);

        await _siber.UpdateYukAsync(new SiberYuk
        {
            YukId = transfer.LoadTransferId!,
            DurumId = refs.LoadStatusSiberId,
            YuklemeTip = refs.LoadTypeCode,
            FirmaId = refs.CustomerSiberId,
            GondericiId = refs.SenderSiberId,
            AliciId = refs.ReceiverSiberId,
            OdemeSekliId = refs.PaymentTypeSiberId,
            TalimatGelisSekli = refs.InstructionCode,
            IstenenRomorkCins = refs.RomorkTypeCode,
            ToplamAgirlik = transfer.TotalGrossWeight,
            ToplamHacim = transfer.TotalVolume,
            ToplamLademetre = transfer.TotalLademeter,
            UcretAgirlik = transfer.WeightFee,
            ToplamKap = transfer.TotalCap,
            MusteriTemsilcisiAd = user?.SiberName,
            DepartmanId = refs.DepartmentSiberId,
            TalimatGelisTarihi = transfer.RequestArrivalDate?.ToDateTime(TimeOnly.MinValue)
                                 ?? _clock.Now,
            YuklemeUlke = transfer.DepartureCountryId,
            BosaltmaUlke = transfer.TargetCountryId,
            CalismaSekli = transfer.WayOfWorking,
        }, cancellationToken);

        // Koliler: Siber kimliği olanlar güncellenir. Kimliği olmayanlar
        // (yeni eklenen satırlar) burada atlanır — Siber'e ekleme dönüşüm
        // akışının işi, düzenlemede yeni kimlik üretmiyoruz.
        var packages = await _db.LoadTransferPackages.AsNoTracking()
            .Where(p => p.LoadTransferId == transfer.LoadTransferId && p.Yukkoliid != null)
            .ToListAsync(cancellationToken);

        foreach (var package in packages)
        {
            await _siber.UpdateYukKoliAsync(new SiberYukKoli
            {
                YukKoliId = package.Yukkoliid!,
                YukId = transfer.LoadTransferId!,
                KapAdet = package.Quantity,
                KapId = refs.CaseTypeCodes.GetValueOrDefault(ToInt(package.CaseTypeId)),
                En = package.Width,
                Boy = package.Length,
                Yukseklik = package.Height,
                Hacim = package.Volume,
                BurutAgirlik = package.GrossWeight,
                NetAgirlik = package.NetWeight,
                Lademetre = package.Lademeter,
                Istiflenemez = package.Stackable ?? 0,
                MalCinsId = refs.ProductTypeCodes.GetValueOrDefault(package.ProductTypeId ?? 0),
            }, cancellationToken);
        }

        var items = await _db.LoadTransferInvoiceItems.AsNoTracking()
            .Where(i => i.InsertName == transfer.LoadNumberWorkType && i.Modulkalemid != null)
            .ToListAsync(cancellationToken);

        foreach (var item in items)
        {
            await _siber.UpdateModulKalemAsync(new SiberModulKalem
            {
                ModulKalemId = item.Modulkalemid!,
                KalemId = refs.FinancialItemCodes.GetValueOrDefault(item.ItemId ?? 0),
                // Siber'de alış "C", satış "G".
                Gc = item.Buysell == "1" ? "C" : "G",
                FirmaId = refs.AccountSiberIds.GetValueOrDefault(item.AccountId ?? 0),
                ToplamTutar = item.TotalPrice,
                DovizKod = refs.CurrencyCodes.GetValueOrDefault(item.CurrencyCode ?? 0),
                BirimFiyat = item.NetPrice,
                Miktar = item.Quantity,
                Tutar = item.TotalPrice,
            }, cancellationToken);
        }
    }

    /// <summary>
    /// <c>load_transfer_packages.case_type_id</c> metin olarak saklanıyor
    /// (dönüşüm kodu <c>ToString()</c> ile yazıyor); sözlükte aramak için
    /// sayıya çevrilir.
    /// </summary>
    private static int ToInt(string? value) =>
        int.TryParse(value, out var parsed) ? parsed : 0;

    private sealed record SiberRefs(
        string? LoadStatusSiberId, string? LoadTypeCode, string? CustomerSiberId,
        string? SenderSiberId, string? ReceiverSiberId, string? PaymentTypeSiberId,
        string? InstructionCode, string? RomorkTypeCode, string? DepartmentSiberId,
        IReadOnlyDictionary<int, string?> CaseTypeCodes,
        IReadOnlyDictionary<int, string?> ProductTypeCodes,
        IReadOnlyDictionary<int, string?> FinancialItemCodes,
        IReadOnlyDictionary<int, string?> AccountSiberIds,
        IReadOnlyDictionary<int, string?> CurrencyCodes);

    /// <summary>Yerel id'lerin Siber karşılıklarını tek seferde toplar.</summary>
    private async Task<SiberRefs> LoadSiberRefsAsync(
        LoadTransfer transfer, CancellationToken cancellationToken)
    {
        var accounts = await _db.Accounts.AsNoTracking()
            .Where(a => a.SiberId != null)
            .ToDictionaryAsync(a => (int)a.Id, a => a.SiberId, cancellationToken);

        return new SiberRefs(
            // load_status_types tablosunda Siber karşılığı yok; durum kodu
            // yerel id olarak yazılır (dönüşüm akışı da sabit 1 yazıyor).
            transfer.LoadStatusId?.ToString(),
            transfer.LoadTypeId?.ToString(),
            accounts.GetValueOrDefault(transfer.CustomerId ?? 0),
            accounts.GetValueOrDefault(transfer.SenderId ?? 0),
            accounts.GetValueOrDefault(transfer.ReceiverId ?? 0),
            await _db.PaymentTypes.AsNoTracking()
                .Where(p => p.Id == transfer.PaymentTypeId)
                .Select(p => p.SiberId).FirstOrDefaultAsync(cancellationToken),
            await _db.Instructions.AsNoTracking()
                .Where(i => i.Id == transfer.InstructionId)
                .Select(i => i.SiberId).FirstOrDefaultAsync(cancellationToken),
            await _db.RomorkTypes.AsNoTracking()
                .Where(r => r.Id == transfer.RomorkTypeId)
                .Select(r => r.SiberId).FirstOrDefaultAsync(cancellationToken),
            await _db.Departments.AsNoTracking()
                .Where(d => d.Id == transfer.DepartmentId)
                .Select(d => d.SiberId).FirstOrDefaultAsync(cancellationToken),
            await _db.CaseTypes.AsNoTracking()
                .ToDictionaryAsync(c => (int)c.Id, c => c.SiberId, cancellationToken),
            await _db.ProductTypes.AsNoTracking()
                .ToDictionaryAsync(p => (int)p.Id, p => p.SiberId, cancellationToken),
            await _db.FinancialItems.AsNoTracking()
                .ToDictionaryAsync(f => (int)f.Id, f => f.SiberId, cancellationToken),
            accounts,
            await _db.Currencies.AsNoTracking()
                .ToDictionaryAsync(c => (int)c.Id, c => c.Code, cancellationToken));
    }
}
