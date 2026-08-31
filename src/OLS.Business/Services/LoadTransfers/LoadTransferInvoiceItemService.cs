using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using OLS.Business.Common;
using OLS.DataAccess.Context;
using OLS.DataAccess.Entities;

namespace OLS.Business.Services.LoadTransfers;

/// <summary>
/// Yük aktarma fatura kalemleri (<c>load_transfer_invoice_item</c>).
/// olsold: <c>Front\LoadTransferInvoiceItem\LoadTransferInvoiceItemController</c>
///
/// Fatura oluşturma ekranındaki "Kalemler" penceresi bu listeyi okuyor
/// (<c>InvoiceFormInvoiceItems.vue</c>); <c>status</c>, <c>buysell</c> ve
/// <c>account_id</c> ile süzülür.
///
/// Kalemler normalde Siber'den ETL ile gelir; buradaki yazma uçları elle
/// düzeltme içindir ve yalnızca <c>name</c>/<c>siber_id</c> alanlarını taşır
/// (kaynakta da böyle — tabloda <c>name</c> sütunu yok, bkz. aşağıdaki not).
/// </summary>
public interface ILoadTransferInvoiceItemService
{
    Task<object> ListAsync(
        InvoiceItemQuery query, CancellationToken cancellationToken = default);

    Task<InvoiceItemDto?> SingleAsync(long id, CancellationToken cancellationToken = default);

    Task<InvoiceItemDto> CreateAsync(
        string? insertName, string? modulKalemId, CancellationToken cancellationToken = default);

    Task<InvoiceItemDto?> UpdateAsync(
        long id, string? insertName, string? modulKalemId, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(IReadOnlyList<long> ids, CancellationToken cancellationToken = default);
}

public sealed record InvoiceItemQuery(
    string? Search, string? Status, string? Buysell, long? AccountId,
    int? PerPage, int Page, string Path);

public sealed class InvoiceItemDto
{
    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("modulkalemid")] public string? Modulkalemid { get; init; }
    [JsonPropertyName("modulid")] public string? Modulid { get; init; }
    [JsonPropertyName("modulkod")] public string? Modulkod { get; init; }
    [JsonPropertyName("item_id")] public int? ItemId { get; init; }
    [JsonPropertyName("buysell")] public string? Buysell { get; init; }
    [JsonPropertyName("total_price")] public decimal? TotalPrice { get; init; }
    [JsonPropertyName("net_price")] public decimal? NetPrice { get; init; }
    [JsonPropertyName("quantity")] public decimal? Quantity { get; init; }
    [JsonPropertyName("tax_price")] public decimal? TaxPrice { get; init; }
    [JsonPropertyName("tax_rate")] public decimal? TaxRate { get; init; }
    [JsonPropertyName("insert_name")] public string? InsertName { get; init; }
    [JsonPropertyName("description")] public string? Description { get; init; }
    [JsonPropertyName("status")] public string? Status { get; init; }
    [JsonPropertyName("currency_code")] public int? CurrencyCode { get; init; }

    /// <summary>İlişki <c>account_id</c> sütunu EZER — arayüz <c>account_id.name</c> okuyor.</summary>
    [JsonPropertyName("account_id")] public InvoiceItemAccountDto? AccountId { get; init; }
    [JsonPropertyName("currency")] public InvoiceItemCurrencyDto? Currency { get; init; }
    [JsonPropertyName("item")] public InvoiceItemRefDto? Item { get; init; }
    [JsonPropertyName("load_transfer")] public InvoiceItemRefDto? LoadTransfer { get; init; }
}

public sealed class InvoiceItemAccountDto
{
    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("name")] public string? Name { get; init; }
    [JsonPropertyName("email")] public string? Email { get; init; }
}

public sealed class InvoiceItemCurrencyDto
{
    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("name")] public string? Name { get; init; }
    [JsonPropertyName("code")] public string? Code { get; init; }
}

public sealed class InvoiceItemRefDto
{
    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("name")] public string? Name { get; init; }
}

public sealed class LoadTransferInvoiceItemService : ILoadTransferInvoiceItemService
{
    private readonly OlsDbContext _db;
    private readonly IClock _clock;

    public LoadTransferInvoiceItemService(OlsDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<object> ListAsync(
        InvoiceItemQuery query, CancellationToken cancellationToken = default)
    {
        var items = _db.LoadTransferInvoiceItems.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Status))
            items = items.Where(i => i.Status == query.Status);

        if (!string.IsNullOrWhiteSpace(query.Buysell))
            items = items.Where(i => i.Buysell == query.Buysell);

        if (query.AccountId is { } accountId)
            items = items.Where(i => i.AccountId == (int)accountId);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            // Türkçe noktasız I/ı normalizasyonu için bkz. QueryableExtensions.NormalizeTurkish.
            var pattern = $"%{QueryableExtensions.NormalizeTurkish(query.Search)}%";

            // DÜRÜST NOT / performans: burada da LoadTransferService.cs'teki gibi korelasyonlu
            // .Any() yerine önce eşleşen ID'ler materialize edilip Contains() ile birleştiriliyor
            // (Postgres'in parametreli sorguda OR'lu EXISTS alt sorgularını kötü planlaması riski).
            var matchingItemIds = await _db.FinancialItems
                .Where(f => f.Name != null && EF.Functions.Like(f.Name.Replace("İ", "i").Replace("I", "i").Replace("ı", "i").ToLower(), pattern))
                .Select(f => (int)f.Id)
                .ToListAsync(cancellationToken);

            var matchingAccountIds = await _db.Accounts
                .Where(a => a.Name != null && EF.Functions.Like(a.Name.Replace("İ", "i").Replace("I", "i").Replace("ı", "i").ToLower(), pattern))
                .Select(a => (int)a.Id)
                .ToListAsync(cancellationToken);

            // Kaynak: insert_name VEYA ilişkili kalem adı VEYA cari adı.
            items = items.Where(i =>
                (i.InsertName != null && EF.Functions.Like(i.InsertName.Replace("İ", "i").Replace("I", "i").Replace("ı", "i").ToLower(), pattern)) ||
                (i.ItemId != null && matchingItemIds.Contains(i.ItemId.Value)) ||
                (i.AccountId != null && matchingAccountIds.Contains(i.AccountId.Value)));
        }

        var projected = items.OrderByDescending(i => i.Id).Select(Project());

        return await projected.ToPagedOrListAsync(
            query.PerPage, query.Page, query.Path, cancellationToken);
    }

    public async Task<InvoiceItemDto?> SingleAsync(
        long id, CancellationToken cancellationToken = default) =>
        await _db.LoadTransferInvoiceItems.AsNoTracking()
            .Where(i => i.Id == id)
            .Select(Project())
            .FirstOrDefaultAsync(cancellationToken);

    /// <summary>
    /// KAYNAK NOTU: <c>store</c> <c>['name' =&gt; …, 'siber_id' =&gt; …]</c> yazıyor
    /// ama <c>load_transfer_invoice_items</c> tablosunda ne <c>name</c> ne
    /// <c>siber_id</c> sütunu var → PostgreSQL 42703, uç her çağrıda 500
    /// veriyordu. Port aynı alanları tablodaki karşılıklarına yazar:
    /// <c>name → insert_name</c>, <c>siber_id → modulkalemid</c>.
    /// </summary>
    public async Task<InvoiceItemDto> CreateAsync(
        string? insertName, string? modulKalemId, CancellationToken cancellationToken = default)
    {
        var now = _clock.Now;

        var item = new LoadTransferInvoiceItem
        {
            InsertName = insertName,
            Modulkalemid = modulKalemId,
            Status = "pending",
            CreatedAt = now,
            UpdatedAt = now,
        };

        _db.LoadTransferInvoiceItems.Add(item);
        await _db.SaveChangesAsync(cancellationToken);

        return (await SingleAsync(item.Id, cancellationToken))!;
    }

    public async Task<InvoiceItemDto?> UpdateAsync(
        long id, string? insertName, string? modulKalemId,
        CancellationToken cancellationToken = default)
    {
        var item = await _db.LoadTransferInvoiceItems
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

        if (item is null)
            return null;

        item.InsertName = insertName;
        item.Modulkalemid = modulKalemId;
        item.UpdatedAt = _clock.Now;

        await _db.SaveChangesAsync(cancellationToken);

        return await SingleAsync(id, cancellationToken);
    }

    public async Task<bool> DeleteAsync(
        IReadOnlyList<long> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
            return false;

        var items = await _db.LoadTransferInvoiceItems
            .Where(i => ids.Contains(i.Id))
            .ToListAsync(cancellationToken);

        if (items.Count == 0)
            return false;

        _db.LoadTransferInvoiceItems.RemoveRange(items);
        await _db.SaveChangesAsync(cancellationToken);

        return true;
    }

    private System.Linq.Expressions.Expression<Func<LoadTransferInvoiceItem, InvoiceItemDto>> Project() =>
        i => new InvoiceItemDto
        {
            Id = i.Id,
            Modulkalemid = i.Modulkalemid,
            Modulid = i.Modulid,
            Modulkod = i.Modulkod,
            ItemId = i.ItemId,
            Buysell = i.Buysell,
            TotalPrice = i.TotalPrice,
            NetPrice = i.NetPrice,
            Quantity = i.Quantity,
            TaxPrice = i.TaxPrice,
            TaxRate = i.TaxRate,
            InsertName = i.InsertName,
            Description = i.Description,
            Status = i.Status,
            CurrencyCode = i.CurrencyCode,
            AccountId = _db.Accounts.Where(a => a.Id == i.AccountId)
                .Select(a => new InvoiceItemAccountDto { Id = a.Id, Name = a.Name, Email = a.Email })
                .FirstOrDefault(),
            Currency = _db.Currencies.Where(c => c.Id == i.CurrencyCode)
                .Select(c => new InvoiceItemCurrencyDto { Id = c.Id, Name = c.Name, Code = c.Code })
                .FirstOrDefault(),
            Item = _db.FinancialItems.Where(f => f.Id == i.ItemId)
                .Select(f => new InvoiceItemRefDto { Id = f.Id, Name = f.Name })
                .FirstOrDefault(),
            // Kaynak ilişkisi insert_name ↔ load_number_work_type üzerinden.
            LoadTransfer = _db.LoadTransfers.Where(t => t.LoadNumberWorkType == i.InsertName)
                .Select(t => new InvoiceItemRefDto { Id = t.Id, Name = t.LoadNumberWorkType })
                .FirstOrDefault(),
        };
}
