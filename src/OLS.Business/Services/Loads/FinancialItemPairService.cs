using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using OLS.DataAccess.Context;

namespace OLS.Business.Services.Loads;

/// <summary>
/// Navlun alış → satış kalem eşleşmeleri (bkz. <c>FinancialItemPair</c>).
///
/// Liste küçük (bugün 11 satır) ve form açılışında bir kez okunuyor; arayüz
/// alış kalemi seçildiğinde karşılığındaki satış satırını buradan bulup
/// <c>alış × (1 + kâr/100)</c> ile dolduruyor.
/// </summary>
public interface IFinancialItemPairService
{
    Task<IReadOnlyList<FinancialItemPairDto>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Verilen kalemlerden en az biri NAVLUN kalemi mi
    /// (<c>financial_items.is_freight</c>)?
    ///
    /// "Olumlu teklifte en az bir navlun kalemi zorunlu" kuralının kaynağı.
    /// Bayrağa bakılıyor, ada değil — kalem tablosu 47.192 satır ve adı
    /// "NAVLUN" geçen her kalem navlun değil.
    /// </summary>
    Task<bool> AnyFreightAsync(
        IEnumerable<int?> itemIds, CancellationToken cancellationToken = default);
}

public sealed class FinancialItemPairDto
{
    [JsonPropertyName("purchase_item_id")] public long PurchaseItemId { get; init; }
    [JsonPropertyName("purchase_item_name")] public string? PurchaseItemName { get; init; }
    [JsonPropertyName("sale_item_id")] public long SaleItemId { get; init; }
    [JsonPropertyName("sale_item_name")] public string? SaleItemName { get; init; }

    /// <summary>Alışın üzerine eklenecek yüzde (varsayılan 15).</summary>
    [JsonPropertyName("markup_percent")] public decimal MarkupPercent { get; init; }

    /// <summary>
    /// Satış kaleminin <c>type</c>'ı — arayüz otomatik açtığı satırın
    /// Alış/Satış seçimini buna göre ayarlıyor.
    /// </summary>
    [JsonPropertyName("sale_item_type")] public int? SaleItemType { get; init; }

    /// <summary>
    /// Satış kaleminin varsayılan carisi; kalem seçicideki davranışın aynısı
    /// otomatik satırda da geçerli olsun diye taşınıyor.
    /// </summary>
    [JsonPropertyName("sale_default_account_id")] public long? SaleDefaultAccountId { get; init; }
    [JsonPropertyName("sale_default_account_name")] public string? SaleDefaultAccountName { get; init; }
}

public sealed class FinancialItemPairService : IFinancialItemPairService
{
    private readonly OlsDbContext _db;

    public FinancialItemPairService(OlsDbContext db) => _db = db;

    public async Task<IReadOnlyList<FinancialItemPairDto>> ListAsync(
        CancellationToken cancellationToken = default) =>
        await _db.FinancialItemPairs.AsNoTracking()
            .Select(p => new FinancialItemPairDto
            {
                PurchaseItemId = p.PurchaseItemId,
                PurchaseItemName = p.PurchaseItem.Name,
                SaleItemId = p.SaleItemId,
                SaleItemName = p.SaleItem.Name,
                MarkupPercent = p.MarkupPercent,
                SaleItemType = p.SaleItem.Type,
                SaleDefaultAccountId = p.SaleItem.DefaultAccountId,
                SaleDefaultAccountName = p.SaleItem.DefaultAccountName,
            })
            .OrderBy(p => p.PurchaseItemName)
            .ToListAsync(cancellationToken);

    public async Task<bool> AnyFreightAsync(
        IEnumerable<int?> itemIds, CancellationToken cancellationToken = default)
    {
        var ids = itemIds.Where(id => id is > 0).Select(id => (long)id!.Value).Distinct().ToList();

        return ids.Count > 0
            && await _db.FinancialItems.AsNoTracking()
                .AnyAsync(f => ids.Contains(f.Id) && f.IsFreight, cancellationToken);
    }
}
