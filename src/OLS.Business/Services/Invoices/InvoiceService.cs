using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using OLS.Business.Common;
using OLS.Business.Services.Accounts;
using OLS.Business.Services.Loads;
using OLS.DataAccess.Context;
using OLS.DataAccess.Entities;

namespace OLS.Business.Services.Invoices;

/// <summary>
/// Fatura modülü — veritabanı tarafı.
/// olsold: <c>Front\Invoice\InvoiceController</c> + <c>InvoiceFooterController</c>
///
/// PORTLANMAYANLAR (hepsi Uyumsoft SOAP istemcisine bağlı, görev #8):
/// <c>pdf-view/inbox|outbox</c>, <c>draft/send|cancel|approve</c>,
/// <c>accept-or-reject</c>. Bunlar yarım portlanmadı; çünkü Uyumsoft çağrısı
/// olmadan "başarılı" dönmeleri gerçekte fatura göndermedikleri hâlde
/// gönderilmiş izlenimi verirdi.
/// </summary>
public interface IInvoiceService
{
    Task<object> ListAsync(InvoiceListQuery query, CancellationToken cancellationToken = default);
    Task<InvoiceDto?> SingleAsync(long id, CancellationToken cancellationToken = default);
    Task<InvoiceDto?> CreateAsync(InvoiceSaveRequest request, CancellationToken cancellationToken = default);

    /// <summary>Faturayı günceller ve kalem eşlemelerini baştan kurar.</summary>
    Task<bool> UpdateAsync(InvoiceUpdateRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(IReadOnlyList<long> ids, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InvoiceFooterDto>> FootersAsync(
        long invoiceId, string? search, string sortColumn, string sortOrder,
        CancellationToken cancellationToken = default);

    Task<InvoiceFooterDto?> FooterSingleAsync(long id, CancellationToken cancellationToken = default);
    Task<InvoiceFooterDto> FooterCreateAsync(long invoiceId, string value, CancellationToken cancellationToken = default);
    Task<InvoiceFooterDto?> FooterUpdateAsync(long id, long invoiceId, string value, CancellationToken cancellationToken = default);
    Task<bool> FooterDeleteAsync(IReadOnlyList<long> ids, CancellationToken cancellationToken = default);
}

public sealed record InvoiceListQuery(
    string? Search, short? BoxType, long? InvoiceStatusId, bool? CreatedByIntegration,
    int? PerPage, int Page, string Path);

public sealed class InvoiceService : IInvoiceService
{
    /// <summary>
    /// "Onay Bekliyor" durumu. Durum süzgeci verilmediğinde olsold bu durumdaki
    /// faturaları listeden gizler — onlar ayrı "Onay Bekleyen" sayfasında görünür.
    /// </summary>
    private const long StatusPendingApproval = 7;

    /// <summary>Kalem satış/alış durumları (olsold string sabitleri).</summary>
    private const string ItemStatusIssued = "invoice_issued";
    private const string ItemStatusReceived = "invoice_received";

    private readonly OlsDbContext _db;
    private readonly IClock _clock;

    public InvoiceService(OlsDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<object> ListAsync(
        InvoiceListQuery query, CancellationToken cancellationToken = default)
    {
        var invoices = _db.Invoices.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{query.Search}%";
            invoices = invoices.Where(i =>
                (i.InvoiceId != null && EF.Functions.ILike(i.InvoiceId, pattern)) ||
                EF.Functions.ILike(i.TargetTitle, pattern));
        }

        if (query.BoxType is { } boxType)
            invoices = invoices.Where(i => i.BoxType == boxType);

        // Durum verilmişse ona göre; verilmemişse "Onay Bekliyor" gizlenir.
        invoices = query.InvoiceStatusId is { } statusId
            ? invoices.Where(i => i.InvoiceStatusId == statusId)
            : invoices.Where(i => i.InvoiceStatusId != StatusPendingApproval ||
                                  i.InvoiceStatusId == null);

        if (query.CreatedByIntegration is { } byIntegration)
            invoices = invoices.Where(i => i.CreatedByIntegration == byIntegration);

        var projected = invoices
            .OrderByDescending(i => i.Id)
            .Select(Project);

        return await projected.ToPagedOrListAsync(
            query.PerPage, query.Page, query.Path, cancellationToken);
    }

    public async Task<InvoiceDto?> SingleAsync(
        long id, CancellationToken cancellationToken = default)
    {
        return await _db.Invoices.AsNoTracking()
            .Where(i => i.Id == id)
            .Select(Project)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<InvoiceDto?> CreateAsync(
        InvoiceSaveRequest request, CancellationToken cancellationToken = default)
    {
        var account = await _db.Accounts.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.AccountId, cancellationToken);

        if (account is null)
            return null;

        var now = _clock.Now;

        // target_* alanları cariden kopyalanır (kaynaktaki davranış).
        var invoice = new Invoice
        {
            BoxType = request.BoxType!.Value,
            CommercialType = request.CommercialType!.Value,
            AccountId = request.AccountId,
            TargetIdentityNo = account.TaxNumber ?? string.Empty,
            TargetTitle = account.Name ?? string.Empty,
            Message = request.Message,
            InvoiceCreateDate = Normalize(request.InvoiceCreateDate!.Value),
            InvoiceExecutionDate = Normalize(request.InvoiceExecutionDate!.Value),
            OrderDocumentId = request.OrderDocumentId,
            InvoiceTypeId = request.InvoiceTypeId,
            CreatedAt = now,
            UpdatedAt = now,
        };

        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync(cancellationToken);

        return await SingleAsync(invoice.Id, cancellationToken);
    }

    public async Task<bool> UpdateAsync(
        InvoiceUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var invoice = await _db.Invoices
            .FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken);

        if (invoice is null)
            return false;

        var account = await _db.Accounts.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.AccountId, cancellationToken);

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

        invoice.BoxType = request.BoxType!.Value;
        invoice.CommercialType = request.CommercialType!.Value;
        invoice.AccountId = request.AccountId;
        invoice.TargetIdentityNo = account?.TaxNumber ?? string.Empty;
        invoice.TargetTitle = account?.Name ?? string.Empty;
        invoice.Message = request.Message;
        invoice.InvoiceCreateDate = Normalize(request.InvoiceCreateDate!.Value);
        invoice.InvoiceExecutionDate = Normalize(request.InvoiceExecutionDate!.Value);
        invoice.OrderDocumentId = request.OrderDocumentId;
        invoice.InvoiceTypeId = request.InvoiceTypeId;
        invoice.UpdatedAt = _clock.Now;

        // Eşlemeler her güncellemede baştan kurulur (olsold: önce hepsini siler).
        var existing = await _db.LoadTransferInvoiceMaps
            .Where(m => m.InvoiceId == invoice.Id)
            .ToListAsync(cancellationToken);

        _db.LoadTransferInvoiceMaps.RemoveRange(existing);

        if (request.LoadTransferInvoiceMaps is { Count: > 0 } maps)
        {
            var now = _clock.Now;
            var itemIds = maps.Select(m => m.InvoiceItemId).Distinct().ToList();

            var items = await _db.LoadTransferInvoiceItems
                .Where(i => itemIds.Contains(i.Id))
                .ToListAsync(cancellationToken);

            foreach (var map in maps)
            {
                _db.LoadTransferInvoiceMaps.Add(new LoadTransferInvoiceMap
                {
                    LoadTransferId = map.LoadTransferId,
                    InvoiceItemId = map.InvoiceItemId,
                    InvoiceId = invoice.Id,
                    CreatedAt = now,
                    UpdatedAt = now,
                });

                // Kalem faturalandı olarak işaretlenir; alış (buysell = 1) ise
                // "fatura alındı", aksi hâlde "fatura kesildi".
                var item = items.FirstOrDefault(i => i.Id == map.InvoiceItemId);

                if (item is not null && !string.IsNullOrEmpty(item.Buysell))
                {
                    item.Status = item.Buysell == "1" ? ItemStatusReceived : ItemStatusIssued;
                    item.UpdatedAt = now;
                }
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return true;
    }

    public async Task<bool> DeleteAsync(
        IReadOnlyList<long> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
            return false;

        var invoices = await _db.Invoices
            .Where(i => ids.Contains(i.Id))
            .ToListAsync(cancellationToken);

        if (invoices.Count == 0)
            return false;

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

        var maps = await _db.LoadTransferInvoiceMaps
            .Where(m => ids.Contains(m.InvoiceId))
            .ToListAsync(cancellationToken);

        _db.LoadTransferInvoiceMaps.RemoveRange(maps);
        _db.Invoices.RemoveRange(invoices);

        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return true;
    }

    // ── Dipnotlar ───────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<InvoiceFooterDto>> FootersAsync(
        long invoiceId, string? search, string sortColumn, string sortOrder,
        CancellationToken cancellationToken = default)
    {
        var footers = _db.InvoiceFooters.AsNoTracking()
            .Where(f => f.InvoiceId == invoiceId)
            .WhereILike(f => f.Value, search);

        var descending = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);

        footers = sortColumn.ToLowerInvariant() switch
        {
            "value" => descending ? footers.OrderByDescending(f => f.Value) : footers.OrderBy(f => f.Value),
            _ => descending ? footers.OrderByDescending(f => f.Id) : footers.OrderBy(f => f.Id),
        };

        return await footers.Select(ProjectFooter).ToListAsync(cancellationToken);
    }

    public async Task<InvoiceFooterDto?> FooterSingleAsync(
        long id, CancellationToken cancellationToken = default)
    {
        return await _db.InvoiceFooters.AsNoTracking()
            .Where(f => f.Id == id)
            .Select(ProjectFooter)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<InvoiceFooterDto> FooterCreateAsync(
        long invoiceId, string value, CancellationToken cancellationToken = default)
    {
        var now = _clock.Now;

        var footer = new InvoiceFooter
        {
            InvoiceId = invoiceId,
            Value = value,
            CreatedAt = now,
            UpdatedAt = now,
        };

        _db.InvoiceFooters.Add(footer);
        await _db.SaveChangesAsync(cancellationToken);

        return new InvoiceFooterDto { Id = footer.Id, InvoiceId = footer.InvoiceId, Value = footer.Value };
    }

    public async Task<InvoiceFooterDto?> FooterUpdateAsync(
        long id, long invoiceId, string value, CancellationToken cancellationToken = default)
    {
        var footer = await _db.InvoiceFooters.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

        if (footer is null)
            return null;

        footer.InvoiceId = invoiceId;
        footer.Value = value;
        footer.UpdatedAt = _clock.Now;

        await _db.SaveChangesAsync(cancellationToken);

        return new InvoiceFooterDto { Id = footer.Id, InvoiceId = footer.InvoiceId, Value = footer.Value };
    }

    public async Task<bool> FooterDeleteAsync(
        IReadOnlyList<long> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
            return false;

        var footers = await _db.InvoiceFooters
            .Where(f => ids.Contains(f.Id))
            .ToListAsync(cancellationToken);

        if (footers.Count == 0)
            return false;

        _db.InvoiceFooters.RemoveRange(footers);
        await _db.SaveChangesAsync(cancellationToken);

        return true;
    }

    /// <summary>
    /// Laravel timestamp sütunları saat dilimsiz; Kind=Utc yazılamaz.
    /// Gelen tarih hangi biçimde olursa olsun Unspecified'a indirilir.
    /// </summary>
    private static DateTime Normalize(DateTime value) =>
        DateTime.SpecifyKind(value, DateTimeKind.Unspecified);

    private static readonly Expression<Func<InvoiceFooter, InvoiceFooterDto>> ProjectFooter =
        f => new InvoiceFooterDto { Id = f.Id, InvoiceId = f.InvoiceId, Value = f.Value };

    /// <summary>
    /// Liste ve detay AYNI projeksiyonu kullanır — olsold'da detay biraz daha
    /// derin yüklüyordu ama fazladan alanların listede dönmesi arayüzü bozmuyor,
    /// iki ayrı projeksiyon ise sapma riski taşıyor.
    /// </summary>
    private static readonly Expression<Func<Invoice, InvoiceDto>> Project = i => new InvoiceDto
    {
        Id = i.Id,
        InvoiceTypeId = i.InvoiceTypeId,
        InvoiceStatusId = i.InvoiceStatusId,
        AccountId = i.AccountId,
        BoxType = i.BoxType,
        CommercialType = i.CommercialType,
        InvoiceId = i.InvoiceId,
        DocumentId = i.DocumentId,
        TargetIdentityNo = i.TargetIdentityNo,
        TargetTitle = i.TargetTitle,
        EnvelopeIdentifier = i.EnvelopeIdentifier,
        EnvelopeStatusCode = i.EnvelopeStatusCode,
        Message = i.Message,
        InvoiceCreateDate = i.InvoiceCreateDate,
        InvoiceExecutionDate = i.InvoiceExecutionDate,
        PayableAmount = i.PayableAmount,
        TaxAmount = i.TaxAmount,
        TaxExclusiveAmount = i.TaxExclusiveAmount,
        TaxRate = i.TaxRate,
        DocumentCurrencyCode = i.DocumentCurrencyCode,
        ExchangeDate = i.ExchangeDate,
        ExchangeRate = i.ExchangeRate,
        OrderDocumentId = i.OrderDocumentId,
        IsArchived = i.IsArchived,
        CreatedByIntegration = i.CreatedByIntegration,
        CreatedAt = i.CreatedAt,
        UpdatedAt = i.UpdatedAt,

        InvoiceType = i.InvoiceType == null ? null : new InvoiceTypeDto
        {
            Id = i.InvoiceType.Id, Code = i.InvoiceType.Code, Name = i.InvoiceType.Name,
        },
        InvoiceStatus = i.InvoiceStatus == null ? null : new InvoiceStatusDto
        {
            Id = i.InvoiceStatus.Id, EnumValue = i.InvoiceStatus.EnumValue,
            Code = i.InvoiceStatus.Code, Name = i.InvoiceStatus.Name,
        },
        InvoiceAccount = i.Account == null ? null : new AccountRefDto
        {
            Id = i.Account.Id, Name = i.Account.Name, Avatar = i.Account.Avatar,
            Email = i.Account.Email, Phone = i.Account.Phone, Address = i.Account.Address,
            TaxNumber = i.Account.TaxNumber, SiberId = i.Account.SiberId,
        },

        LoadTransferInvoiceMaps = i.LoadTransferInvoiceMaps
            .Select(m => new LoadTransferInvoiceMapDto
            {
                Id = m.Id,
                LoadTransferId = m.LoadTransferId,
                InvoiceItemId = m.InvoiceItemId,
                InvoiceId = m.InvoiceId,
                LoadTransfer = m.LoadTransfer == null ? null : new LoadTransferRefDto
                {
                    Id = m.LoadTransfer.Id,
                    LoadNumber = m.LoadTransfer.LoadNumber,
                    LoadNumberWorkType = m.LoadTransfer.LoadNumberWorkType,
                },
                LoadTransferInvoiceItem = m.InvoiceItem == null ? null : new LoadTransferInvoiceItemDto
                {
                    Id = m.InvoiceItem.Id,
                    ItemId = m.InvoiceItem.ItemId,
                    Buysell = m.InvoiceItem.Buysell,
                    AccountId = m.InvoiceItem.AccountId,
                    Quantity = m.InvoiceItem.Quantity,
                    NetPrice = m.InvoiceItem.NetPrice,
                    TaxPrice = m.InvoiceItem.TaxPrice,
                    TaxRate = m.InvoiceItem.TaxRate,
                    TotalPrice = m.InvoiceItem.TotalPrice,
                    Description = m.InvoiceItem.Description,
                    InsertName = m.InvoiceItem.InsertName,
                    Status = m.InvoiceItem.Status,
                    Modulkalemid = m.InvoiceItem.Modulkalemid,
                },
            })
            .ToList(),
    };
}
