using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using OLS.Business.Common;
using OLS.Business.Services.Accounts;
using OLS.Business.Services.Loads;
using OLS.DataAccess.Context;

namespace OLS.Business.Services.LoadTransfers;

/// <summary>
/// Yük Aktarma modülü — okuma tarafı.
///
/// olsold: <c>Front\LoadTransfer\LoadTransferController</c>
///
/// ÖNEMLİ: Siber'den içe aktarılan GEÇMİŞ yük kayıtları bu tabloda tutulur
/// (<c>skn_yuk</c> -> <c>load_transfers</c>). Yük/Teklif modülündeki
/// <c>loads</c> tablosu ise yeni sistemde açılan kayıtlardır. "Önceki yükler"
/// bu modülde görünür.
/// </summary>
public interface ILoadTransferService
{
    Task<object> ListAsync(LoadTransferListQuery query, CancellationToken cancellationToken = default);
    Task<LoadTransferDetailDto?> SingleAsync(long id, CancellationToken cancellationToken = default);
}

public sealed record LoadTransferListQuery(
    string? Search, int? WorkTypeId, int? PerPage, int Page, string Path);

/// <summary>
/// Liste yanıtı. olsold yalnızca beş sütun seçip üç ilişkiyi yüklüyordu;
/// aynı dar şekli koruyoruz (liste ekranı büyük tabloda hızlı açılsın diye).
/// </summary>
public sealed class LoadTransferListItemDto
{
    [JsonPropertyName("id")] public long Id { get; init; }

    [JsonPropertyName("load_number_work_type")]
    public string? LoadNumberWorkType { get; init; }

    // İlişkiler aynı adlı sütunları ezer
    [JsonPropertyName("load_status_id")] public LoadStatusDto? LoadStatusId { get; init; }
    [JsonPropertyName("customer_id")] public NamedRefDto? CustomerId { get; init; }
    [JsonPropertyName("sender_id")] public NamedRefDto? SenderId { get; init; }
}

public sealed class LoadStatusDto
{
    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("name")] public string? Name { get; init; }
    [JsonPropertyName("load_status_id")] public int? LoadStatusId { get; init; }
    [JsonPropertyName("order_no")] public int? OrderNo { get; init; }
}

public sealed class LoadTransferDetailDto
{
    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("load_transfer_id")] public string? LoadTransferId { get; init; }
    [JsonPropertyName("load_number")] public string? LoadNumber { get; init; }
    [JsonPropertyName("load_number_work_type")] public string? LoadNumberWorkType { get; init; }
    [JsonPropertyName("connected_load_number")] public string? ConnectedLoadNumber { get; init; }
    [JsonPropertyName("total_gross_weight")] public decimal? TotalGrossWeight { get; init; }
    [JsonPropertyName("total_volume")] public decimal? TotalVolume { get; init; }
    [JsonPropertyName("total_lademeter")] public decimal? TotalLademeter { get; init; }
    [JsonPropertyName("total_cap")] public decimal? TotalCap { get; init; }
    [JsonPropertyName("in_truck")] public int? InTruck { get; init; }
    [JsonPropertyName("in_tail")] public int? InTail { get; init; }
    [JsonPropertyName("cmr_waiting")] public int? CmrWaiting { get; init; }
    [JsonPropertyName("fcr_waiting")] public int? FcrWaiting { get; init; }
    [JsonPropertyName("instruction_arrival_date")] public DateOnly? InstructionArrivalDate { get; init; }
    [JsonPropertyName("request_arrival_date")] public DateOnly? RequestArrivalDate { get; init; }
    [JsonPropertyName("readiness_date")] public DateOnly? ReadinessDate { get; init; }
    [JsonPropertyName("date_of_receipt_customer")] public DateOnly? DateOfReceiptCustomer { get; init; }
    [JsonPropertyName("siber_id")] public string? SiberId { get; init; }
    [JsonPropertyName("created_at")] public DateTime? CreatedAt { get; init; }

    [JsonPropertyName("load_status_id")] public LoadStatusDto? LoadStatusId { get; init; }
    [JsonPropertyName("customer_id")] public NamedRefDto? CustomerId { get; init; }
    [JsonPropertyName("sender_id")] public NamedRefDto? SenderId { get; init; }
    [JsonPropertyName("receiver_id")] public NamedRefDto? ReceiverId { get; init; }
    [JsonPropertyName("work_type")] public NamedRefDto? WorkType { get; init; }
    [JsonPropertyName("load_type_id")] public NamedRefDto? LoadTypeId { get; init; }
    [JsonPropertyName("payment_type_id")] public NamedRefDto? PaymentTypeId { get; init; }
    [JsonPropertyName("department_id")] public NamedRefDto? DepartmentId { get; init; }

    /// <summary>
    /// DİKKAT: sütun adı <c>customer_representative_name</c> ama içeriği bir
    /// KULLANICI KİMLİĞİ (int) — olsold'da da aynı yanıltıcı adlandırma var.
    /// olsold: <c>LoadFormDrawer.vue</c> "Görevliler" sekmesi.
    /// </summary>
    [JsonPropertyName("customer_representative")] public MappedUserDto? CustomerRepresentative { get; init; }
    [JsonPropertyName("second_customer_representative")] public MappedUserDto? SecondCustomerRepresentative { get; init; }

    /// <summary>
    /// olsold: <c>load_data.load_belongs</c> — Yük'ün dönüştüğü ORİJİNAL Teklif.
    /// Dosya Arşivi bu Teklif'in <c>load_file</c> kayıtlarını gösterir (Yük'ün
    /// kendi dosya tablosu yok — kaynakta da yok, bkz. LoadFormDrawer.vue
    /// updateLoadFiles: <c>load_id</c> gönderiyor, <c>load_transfer_id</c> değil).
    /// Eşleme <c>load_number_work_type</c> ↔ <c>loads.load_number</c> ile
    /// (dönüşüm sırasında yazılan aynı değer — bkz. LoadTransferWriteService).
    /// </summary>
    [JsonPropertyName("load_id")] public long? OriginalLoadId { get; init; }
    [JsonPropertyName("load_file")] public IReadOnlyList<LoadFileDto> LoadFile { get; init; } = [];

    [JsonPropertyName("load_transfer_package")]
    public IReadOnlyList<LoadTransferPackageDto> LoadTransferPackage { get; init; } = [];

    [JsonPropertyName("load_transfer_invoice_item")]
    public IReadOnlyList<LoadTransferInvoiceItemDto> LoadTransferInvoiceItem { get; init; } = [];
}

public sealed class LoadTransferPackageDto
{
    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("yukkoliid")] public string? Yukkoliid { get; init; }
    [JsonPropertyName("quantity")] public int? Quantity { get; init; }
    [JsonPropertyName("gross_weight")] public decimal? GrossWeight { get; init; }
    [JsonPropertyName("net_weight")] public decimal? NetWeight { get; init; }
    [JsonPropertyName("volume")] public decimal? Volume { get; init; }
    [JsonPropertyName("lademeter")] public decimal? Lademeter { get; init; }
    [JsonPropertyName("width")] public decimal? Width { get; init; }
    [JsonPropertyName("length")] public decimal? Length { get; init; }
    [JsonPropertyName("height")] public decimal? Height { get; init; }
    [JsonPropertyName("stackable")] public int? Stackable { get; init; }
    [JsonPropertyName("product_type_id")] public NamedRefDto? ProductTypeId { get; init; }
}

public sealed class LoadTransferInvoiceItemDto
{
    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("modulkalemid")] public string? Modulkalemid { get; init; }
    [JsonPropertyName("buysell")] public string? Buysell { get; init; }
    [JsonPropertyName("net_price")] public decimal? NetPrice { get; init; }
    [JsonPropertyName("total_price")] public decimal? TotalPrice { get; init; }
    [JsonPropertyName("quantity")] public decimal? Quantity { get; init; }
    [JsonPropertyName("description")] public string? Description { get; init; }
    [JsonPropertyName("status")] public string Status { get; init; } = string.Empty;
    [JsonPropertyName("item_id")] public NamedRefDto? ItemId { get; init; }
    [JsonPropertyName("account_id")] public NamedRefDto? AccountId { get; init; }
    [JsonPropertyName("currency_code")] public CurrencyDto? CurrencyCode { get; init; }
}

public sealed class LoadTransferService : ILoadTransferService
{
    private readonly OlsDbContext _db;

    public LoadTransferService(OlsDbContext db) => _db = db;

    public async Task<object> ListAsync(
        LoadTransferListQuery query, CancellationToken cancellationToken = default)
    {
        var transfers = _db.LoadTransfers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{query.Search}%";
            transfers = transfers.Where(t =>
                EF.Functions.ILike(t.LoadNumberWorkType!, pattern));
        }

        if (query.WorkTypeId is { } workTypeId)
            transfers = transfers.Where(t => t.WorkType == workTypeId);

        var projected = transfers
            .OrderByDescending(t => t.Id)
            .Select(t => new LoadTransferListItemDto
            {
                Id = t.Id,
                LoadNumberWorkType = t.LoadNumberWorkType,
                LoadStatusId = _db.LoadStatusTypes
                    .Where(s => s.Id == t.LoadStatusId)
                    .Select(s => new LoadStatusDto
                    {
                        Id = s.Id, Name = s.Name,
                        LoadStatusId = s.LoadStatusId, OrderNo = s.OrderNo,
                    })
                    .FirstOrDefault(),
                CustomerId = _db.Accounts.Where(a => a.Id == t.CustomerId)
                    .Select(a => new NamedRefDto { Id = a.Id, Name = a.Name })
                    .FirstOrDefault(),
                SenderId = _db.Accounts.Where(a => a.Id == t.SenderId)
                    .Select(a => new NamedRefDto { Id = a.Id, Name = a.Name })
                    .FirstOrDefault(),
            });

        return await projected.ToPagedOrListAsync(
            query.PerPage, query.Page, query.Path, cancellationToken);
    }

    public async Task<LoadTransferDetailDto?> SingleAsync(
        long id, CancellationToken cancellationToken = default)
    {
        var t = await _db.LoadTransfers.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (t is null)
            return null;

        var originalLoadId = t.LoadNumberWorkType is null
            ? (long?)null
            : await _db.Loads.AsNoTracking()
                .Where(l => l.LoadNumber == t.LoadNumberWorkType)
                .Select(l => (long?)l.Id)
                .FirstOrDefaultAsync(cancellationToken);

        return new LoadTransferDetailDto
        {
            Id = t.Id,
            LoadTransferId = t.LoadTransferId,
            LoadNumber = t.LoadNumber,
            LoadNumberWorkType = t.LoadNumberWorkType,
            ConnectedLoadNumber = t.ConnectedLoadNumber,
            TotalGrossWeight = t.TotalGrossWeight,
            TotalVolume = t.TotalVolume,
            TotalLademeter = t.TotalLademeter,
            TotalCap = t.TotalCap,
            InTruck = t.InTruck,
            InTail = t.InTail,
            CmrWaiting = t.CmrWaiting,
            FcrWaiting = t.FcrWaiting,
            InstructionArrivalDate = t.InstructionArrivalDate,
            RequestArrivalDate = t.RequestArrivalDate,
            ReadinessDate = t.ReadinessDate,
            DateOfReceiptCustomer = t.DateOfReceiptCustomer,
            SiberId = t.SiberId,
            CreatedAt = t.CreatedAt,

            LoadStatusId = await _db.LoadStatusTypes.AsNoTracking()
                .Where(s => s.Id == t.LoadStatusId)
                .Select(s => new LoadStatusDto
                {
                    Id = s.Id, Name = s.Name, LoadStatusId = s.LoadStatusId, OrderNo = s.OrderNo,
                })
                .FirstOrDefaultAsync(cancellationToken),

            CustomerId = await AccountRefAsync(t.CustomerId, cancellationToken),
            SenderId = await AccountRefAsync(t.SenderId, cancellationToken),
            ReceiverId = await AccountRefAsync(t.ReceiverId, cancellationToken),

            WorkType = await _db.WorkTypes.AsNoTracking()
                .Where(w => w.Id == t.WorkType)
                .Select(w => new NamedRefDto { Id = w.Id, Name = w.Name, Code = w.Code })
                .FirstOrDefaultAsync(cancellationToken),

            LoadTypeId = await _db.LoadingTypes.AsNoTracking()
                .Where(l => l.Id == t.LoadTypeId)
                .Select(l => new NamedRefDto { Id = l.Id, Name = l.Name, Code = l.Code })
                .FirstOrDefaultAsync(cancellationToken),

            PaymentTypeId = await _db.PaymentTypes.AsNoTracking()
                .Where(p => p.Id == t.PaymentTypeId)
                .Select(p => new NamedRefDto { Id = p.Id, Name = p.Name, Code = p.Code })
                .FirstOrDefaultAsync(cancellationToken),

            DepartmentId = await _db.Departments.AsNoTracking()
                .Where(d => d.Id == t.DepartmentId)
                .Select(d => new NamedRefDto { Id = d.Id, Name = d.Name })
                .FirstOrDefaultAsync(cancellationToken),

            CustomerRepresentative = await UserRefAsync(t.CustomerRepresentativeName, cancellationToken),
            SecondCustomerRepresentative = await UserRefAsync(t.SecondCustomerRepresentativeName, cancellationToken),

            OriginalLoadId = originalLoadId,
            LoadFile = originalLoadId is null
                ? []
                : await _db.LoadFiles.AsNoTracking()
                    .Where(f => f.LoadId == (int)originalLoadId.Value)
                    .Select(f => new LoadFileDto
                    {
                        Id = f.Id, LoadId = f.LoadId, File = f.File,
                        MimeType = f.MimeType, OrgName = f.OrgName, CreatedAt = f.CreatedAt,
                    })
                    .ToListAsync(cancellationToken),

            // Koli ve fatura kalemleri Siber kimliği üzerinden bağlanır
            // (load_transfers.load_transfer_id metin sütunu).
            LoadTransferPackage = await _db.LoadTransferPackages.AsNoTracking()
                .Where(p => p.LoadTransferId == t.LoadTransferId)
                .Select(p => new LoadTransferPackageDto
                {
                    Id = p.Id, Yukkoliid = p.Yukkoliid, Quantity = p.Quantity,
                    GrossWeight = p.GrossWeight, NetWeight = p.NetWeight, Volume = p.Volume,
                    Lademeter = p.Lademeter, Width = p.Width, Length = p.Length,
                    Height = p.Height, Stackable = p.Stackable,
                    ProductTypeId = _db.ProductTypes.Where(x => x.Id == p.ProductTypeId)
                        .Select(x => new NamedRefDto { Id = x.Id, Name = x.Name })
                        .FirstOrDefault(),
                })
                .ToListAsync(cancellationToken),

            LoadTransferInvoiceItem = await _db.LoadTransferInvoiceItems.AsNoTracking()
                .Where(i => i.InsertName == t.LoadNumberWorkType)
                .Select(i => new LoadTransferInvoiceItemDto
                {
                    Id = i.Id, Modulkalemid = i.Modulkalemid, Buysell = i.Buysell,
                    NetPrice = i.NetPrice, TotalPrice = i.TotalPrice, Quantity = i.Quantity,
                    Description = i.Description, Status = i.Status,
                    ItemId = _db.FinancialItems.Where(f => f.Id == i.ItemId)
                        .Select(f => new NamedRefDto { Id = f.Id, Name = f.Name })
                        .FirstOrDefault(),
                    AccountId = _db.Accounts.Where(a => a.Id == i.AccountId)
                        .Select(a => new NamedRefDto { Id = a.Id, Name = a.Name })
                        .FirstOrDefault(),
                    CurrencyCode = _db.Currencies.Where(c => c.Id == i.CurrencyCode)
                        .Select(c => new CurrencyDto { Id = c.Id, Name = c.Name, Code = c.Code })
                        .FirstOrDefault(),
                })
                .ToListAsync(cancellationToken),
        };
    }

    private async Task<NamedRefDto?> AccountRefAsync(int? id, CancellationToken cancellationToken) =>
        id is null
            ? null
            : await _db.Accounts.AsNoTracking()
                .Where(a => a.Id == id)
                .Select(a => new NamedRefDto { Id = a.Id, Name = a.Name })
                .FirstOrDefaultAsync(cancellationToken);

    private async Task<MappedUserDto?> UserRefAsync(int? userId, CancellationToken cancellationToken) =>
        userId is null
            ? null
            : await _db.Users.AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new MappedUserDto
                {
                    Id = u.Id, Name = u.Name ?? string.Empty, Surname = u.Surname ?? string.Empty,
                    Email = u.Email ?? string.Empty, Avatar = u.Avatar, SiberCode = u.SiberCode,
                })
                .FirstOrDefaultAsync(cancellationToken);
}
