using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using OLS.Business.Common;
using OLS.DataAccess.Context;
using OLS.DataAccess.Entities;

namespace OLS.Business.Services.Loads;

/// <summary>
/// AI çıktısından teklif oluşturma.
/// olsold: <c>LoadController::saveAi</c> — <c>POST /load/saveAi</c>
///
/// OpenAI çağrısı BURADA YAPILMAZ: arayüz (<c>Offer.vue</c>) e-postayı
/// tarayıcıda modele gönderir, dönen JSON'u bu uca yollar. Yani bu uç
/// tamamen veritabanı işidir, dış servise dokunmaz.
///
/// AI id değil <b>ad</b> döndürür ("İhracat", "Türkiye", "Peşin"); adlar
/// ILIKE ile eşlenir, eşleşmeyen alan boş bırakılır. Tek istisna müşteri:
/// bulunamazsa yeni cari açılır (kaynaktaki davranış).
/// </summary>
public interface ILoadAiImportService
{
    Task<LoadAiImportResult> CreateAsync(
        LoadAiRequest request, CancellationToken cancellationToken = default);
}

public sealed record LoadAiImportResult(long LoadId, IReadOnlyList<string> Unresolved);

public sealed class LoadAiRequest
{
    [JsonPropertyName("customer_id")] public string? CustomerName { get; set; }
    [JsonPropertyName("work_type_id")] public string? WorkTypeName { get; set; }
    [JsonPropertyName("payment_type_id")] public string? PaymentTypeName { get; set; }
    [JsonPropertyName("target_country_id")] public string? TargetCountryName { get; set; }
    [JsonPropertyName("departure_country_id")] public string? DepartureCountryName { get; set; }
    [JsonPropertyName("currency")] public string? CurrencyName { get; set; }

    [JsonPropertyName("loading_type_id")] public int? LoadingTypeId { get; set; }
    [JsonPropertyName("load_transfer_type_id")] public int? LoadTransferTypeId { get; set; }
    [JsonPropertyName("instruction_id")] public int? InstructionId { get; set; }
    [JsonPropertyName("romork_type_id")] public int? RomorkTypeId { get; set; }
    [JsonPropertyName("sender_id")] public int? SenderId { get; set; }
    [JsonPropertyName("receiver_id")] public int? ReceiverId { get; set; }
    [JsonPropertyName("agent_id")] public int? AgentId { get; set; }
    [JsonPropertyName("company_pay_freight_id")] public int? CompanyPayFreightId { get; set; }
    [JsonPropertyName("transit_country_id")] public string? TransitCountryId { get; set; }
    [JsonPropertyName("department_id")] public int? DepartmentId { get; set; }

    [JsonPropertyName("payer_company")] public string? PayerCompany { get; set; }
    [JsonPropertyName("description")] public string? Description { get; set; }

    [JsonPropertyName("offer_date")] public DateOnly? OfferDate { get; set; }
    [JsonPropertyName("offer_validity_date")] public DateOnly? OfferValidityDate { get; set; }
    [JsonPropertyName("marketing_notification_date")] public DateOnly? MarketingNotificationDate { get; set; }

    [JsonPropertyName("front_transportation_by_us")] public int? FrontTransportationByUs { get; set; }
    [JsonPropertyName("final_transportation_by_us")] public int? FinalTransportationByUs { get; set; }

    // Tek finansal kalem (AI toplam fiyat verirse).
    [JsonPropertyName("total_price")] public decimal? TotalPrice { get; set; }
    [JsonPropertyName("net_price")] public decimal? NetPrice { get; set; }
    [JsonPropertyName("tax_price")] public decimal? TaxPrice { get; set; }
    [JsonPropertyName("quantity")] public int? Quantity { get; set; }
    [JsonPropertyName("transport_type_id")] public int? TransportTypeId { get; set; }
    [JsonPropertyName("status")] public int? Status { get; set; }
    [JsonPropertyName("order")] public int? Order { get; set; }

    [JsonPropertyName("products")] public List<ProductInput>? Products { get; set; }
    [JsonPropertyName("load_charge_person")] public List<ChargePersonInput>? LoadChargePerson { get; set; }

    public sealed class ProductInput
    {
        [JsonPropertyName("product_type_id")] public string? ProductTypeName { get; set; }
        [JsonPropertyName("case_type_id")] public int? CaseTypeId { get; set; }
        [JsonPropertyName("quantity")] public int? Quantity { get; set; }
        [JsonPropertyName("width")] public decimal? Width { get; set; }
        [JsonPropertyName("height")] public decimal? Height { get; set; }
        [JsonPropertyName("length")] public decimal? Length { get; set; }
        [JsonPropertyName("gross_weight")] public decimal? GrossWeight { get; set; }
        [JsonPropertyName("net_weight")] public decimal? NetWeight { get; set; }
        [JsonPropertyName("volume")] public decimal? Volume { get; set; }
        [JsonPropertyName("lademeter")] public decimal? Lademeter { get; set; }
        [JsonPropertyName("stackable")] public int? Stackable { get; set; }
    }

    public sealed class ChargePersonInput
    {
        [JsonPropertyName("user_id")] public int? UserId { get; set; }
        [JsonPropertyName("user_type")] public int? UserType { get; set; }
    }
}

public sealed class LoadAiImportService : ILoadAiImportService
{
    /// <summary>Teklif durumu (olsold sabiti).</summary>
    private const int OfferStatusTypeId = 4;

    /// <summary>
    /// Yeni açılan carinin tipi: 1 = Müşteri (kaynaktaki sabit).
    /// DİKKAT: <c>accounts</c> tablosunda <c>account_type_id</c> sütunu YOK —
    /// tip <c>account_type_mappings</c> ara tablosunda tutulur.
    /// </summary>
    private const int CustomerAccountTypeId = 1;

    private readonly OlsDbContext _db;
    private readonly IClock _clock;

    public LoadAiImportService(OlsDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<LoadAiImportResult> CreateAsync(
        LoadAiRequest request, CancellationToken cancellationToken = default)
    {
        var now = _clock.Now;
        var today = DateOnly.FromDateTime(now);
        var unresolved = new List<string>();

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

        var customerId = await ResolveCustomerAsync(request.CustomerName, now, cancellationToken);

        var workTypeId = await MatchByNameAsync(
            request.WorkTypeName, "work_type_id", unresolved,
            (name, ct) => _db.WorkTypes.AsNoTracking()
                .Where(w => w.Name != null && EF.Functions.ILike(w.Name, name))
                .Select(w => (int?)w.Id).FirstOrDefaultAsync(ct),
            cancellationToken);

        var paymentTypeId = await MatchByNameAsync(
            request.PaymentTypeName, "payment_type_id", unresolved,
            (name, ct) => _db.PaymentTypes.AsNoTracking()
                .Where(p => p.Name != null && EF.Functions.ILike(p.Name, name))
                .Select(p => (int?)p.Id).FirstOrDefaultAsync(ct),
            cancellationToken);

        var targetCountryId = await MatchCountryAsync(
            request.TargetCountryName, "target_country_id", unresolved, cancellationToken);

        var departureCountryId = await MatchCountryAsync(
            request.DepartureCountryName, "departure_country_id", unresolved, cancellationToken);

        var load = new Load
        {
            WorkTypeId = workTypeId,
            LoadingTypeId = request.LoadingTypeId,
            PaymentTypeId = paymentTypeId,
            StatusTypeId = OfferStatusTypeId,

            // Tarih gelmezse bugün (kaynak da öyle).
            OfferDate = request.OfferDate ?? today,
            OfferValidityDate = request.OfferValidityDate ?? today,
            MarketingNotificationDate = request.MarketingNotificationDate ?? today,

            LoadTransferTypeId = request.LoadTransferTypeId,
            InstructionId = request.InstructionId,
            RomorkTypeId = request.RomorkTypeId,
            CustomerId = customerId,
            SenderId = request.SenderId,
            ReceiverId = request.ReceiverId,
            CompanyPayFreightId = request.CompanyPayFreightId,
            AgentId = request.AgentId,
            PayerCompany = request.PayerCompany,
            Description = request.Description,
            DepartureCountryId = departureCountryId,
            TransitCountryId = ParseGuid(request.TransitCountryId),
            TargetCountryId = targetCountryId,
            DepartmentId = request.DepartmentId,
            FrontTransportationByUs = request.FrontTransportationByUs ?? 0,
            FinalTransportationByUs = request.FinalTransportationByUs ?? 0,
            CreatedAt = now,
            UpdatedAt = now,
        };

        _db.Loads.Add(load);
        await _db.SaveChangesAsync(cancellationToken);

        await AddProductsAsync(load.Id, request.Products, now, unresolved, cancellationToken);
        AddFinancialItem(load.Id, request, await ResolveCurrencyAsync(
            request.CurrencyName, "currency", unresolved, cancellationToken), now);
        AddChargePeople(load.Id, request.LoadChargePerson, now);

        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return new LoadAiImportResult(load.Id, unresolved);
    }

    /// <summary>
    /// Müşteri adı eşleşmezse YENİ CARİ açılır — diğer alanlardan farklı olarak
    /// boş bırakılmaz (kaynaktaki davranış). AI'nın ürettiği ada güvenildiği
    /// için yanlış yazımda mükerrer cari oluşabilir; kaynak da böyle.
    /// </summary>
    private async Task<int?> ResolveCustomerAsync(
        string? name, DateTime now, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var existing = await _db.Accounts
            .Where(a => a.Name != null && EF.Functions.ILike(a.Name, $"%{name}%"))
            .Select(a => (int?)a.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is not null)
            return existing;

        // KAYNAKTAN AYRILAN NOKTA: olsold burada
        // `Account::create([... 'account_type_id' => 1 ...])` yazıyordu, ama
        // accounts tablosunda böyle bir sütun yok → PostgreSQL INSERT'i
        // reddediyor ve çağrı 500 veriyor. Üstelik bu satır try bloğunun
        // DIŞINDA, yani hata yakalanmıyor. Sonuç: e-postadan gelen YENİ bir
        // müşteri adıyla saveAi her seferinde patlıyordu.
        // Tip, ait olduğu yere — account_type_mappings'e — yazılır.
        var account = new Account
        {
            Name = name,
            Discount = 0,
            CreatedAt = now,
            UpdatedAt = now,
        };

        _db.Accounts.Add(account);
        await _db.SaveChangesAsync(cancellationToken);

        _db.AccountTypeMappings.Add(new AccountTypeMapping
        {
            AccountId = (int)account.Id,
            AccountTypeId = CustomerAccountTypeId,
            CreatedAt = now,
            UpdatedAt = now,
        });

        await _db.SaveChangesAsync(cancellationToken);

        return (int)account.Id;
    }

    private async Task AddProductsAsync(
        long loadId, IReadOnlyList<LoadAiRequest.ProductInput>? products, DateTime now,
        List<string> unresolved, CancellationToken cancellationToken)
    {
        if (products is not { Count: > 0 })
            return;

        foreach (var product in products)
        {
            var productTypeId = await MatchByNameAsync(
                product.ProductTypeName, "products[].product_type_id", unresolved,
                (name, ct) => _db.ProductTypes.AsNoTracking()
                    .Where(p => p.Name != null && EF.Functions.ILike(p.Name, name))
                    .Select(p => (int?)p.Id).FirstOrDefaultAsync(ct),
                cancellationToken);

            // KAYNAKTAN AYRILAN NOKTA: olsold burada dizi elemanına NESNE
            // erişimi yapıyordu (`$item->quantity`), oysa JSON gövdesi diziye
            // çözülüyor. Sonuç: ürün satırları load_id dışında TÜM ALANLARI
            // BOŞ kaydediliyordu. Alanlar artık gerçekten okunuyor.
            _db.LoadContents.Add(new LoadContent
            {
                LoadId = loadId,
                ProductTypeId = productTypeId,
                CaseTypeId = product.CaseTypeId,
                Quantity = product.Quantity,
                Width = product.Width,
                Height = product.Height,
                Length = product.Length,
                GrossWeight = product.GrossWeight,
                NetWeight = product.NetWeight,
                Volume = product.Volume,
                Lademeter = product.Lademeter,
                Stackable = product.Stackable,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }
    }

    /// <summary>AI toplam fiyat verirse tek bir ALIŞ kalemi (buysell = 1) yazılır.</summary>
    private void AddFinancialItem(long loadId, LoadAiRequest request, int? currencyId, DateTime now)
    {
        if (request.TotalPrice is null)
            return;

        _db.LoadFinancialItems.Add(new LoadFinancialItem
        {
            LoadId = loadId,
            Buysell = 1,
            Quantity = request.Quantity,
            TransportTypeId = request.TransportTypeId,
            Status = request.Status,
            Order = request.Order,
            NetPrice = request.NetPrice,
            TaxPrice = request.TaxPrice,
            TotalPrice = request.TotalPrice,
            Currency = currencyId,
            CreatedAt = now,
            UpdatedAt = now,
        });
    }

    private void AddChargePeople(
        long loadId, IReadOnlyList<LoadAiRequest.ChargePersonInput>? people, DateTime now)
    {
        if (people is not { Count: > 0 })
            return;

        foreach (var person in people)
        {
            _db.LoadChargePeople.Add(new LoadChargePerson
            {
                LoadId = (int)loadId,
                UserId = person.UserId,
                UserType = person.UserType,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }
    }

    // ── Ad → id eşleme ──────────────────────────────────────────────────────

    /// <summary>
    /// Ada göre ILIKE eşlemesi. Eşleşmezse null döner ve alan
    /// <paramref name="unresolved"/> listesine yazılır — kaynak sessizce
    /// geçiyordu, port hangi alanların boş kaldığını bildirir.
    /// </summary>
    private static async Task<int?> MatchByNameAsync(
        string? name, string field, List<string> unresolved,
        Func<string, CancellationToken, Task<int?>> lookup,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var match = await lookup($"%{name}%", cancellationToken);

        if (match is null)
            unresolved.Add($"{field}: \"{name}\"");

        return match;
    }

    private async Task<Guid?> MatchCountryAsync(
        string? name, string field, List<string> unresolved, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var match = await _db.Countries.AsNoTracking()
            .Where(c => c.Name != null && EF.Functions.ILike(c.Name, $"%{name}%"))
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (match is null)
            unresolved.Add($"{field}: \"{name}\"");

        return match;
    }

    private async Task<int?> ResolveCurrencyAsync(
        string? name, string field, List<string> unresolved, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        // AI kod ("EUR") ya da ad ("Euro") döndürebiliyor; ikisi de denenir.
        var match = await _db.Currencies.AsNoTracking()
            .Where(c => (c.Name != null && EF.Functions.ILike(c.Name, $"%{name}%")) ||
                        (c.Code != null && EF.Functions.ILike(c.Code, name)))
            .Select(c => (int?)c.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (match is null)
            unresolved.Add($"{field}: \"{name}\"");

        return match;
    }

    private static Guid? ParseGuid(string? value) =>
        Guid.TryParse(value, out var parsed) ? parsed : null;
}
