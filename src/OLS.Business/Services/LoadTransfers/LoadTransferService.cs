using System.Globalization;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using OLS.Business.Common;
using OLS.Business.Services.Authorization;
using OLS.Business.Services.Accounts;
using OLS.Business.Services.Loads;
using OLS.DataAccess.Context;
using OLS.DataAccess.Siber;

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
    string? Search, int? WorkTypeId, DateOnly? DateFrom, DateOnly? DateTo, int? PerPage, int Page, string Path,
    int? CustomerId = null, int? SenderId = null, int? ReceiverId = null, int? AssignedUserId = null,
    int? StatusId = null, long? CaseTypeId = null, string? FinancialItem = null, decimal? Weight = null);

/// <summary>
/// Liste yanıtı. olsold yalnızca beş sütun seçip üç ilişkiyi yüklüyordu;
/// aynı dar şekli koruyoruz (liste ekranı büyük tabloda hızlı açılsın diye).
/// </summary>
public sealed class LoadTransferListItemDto
{
    [JsonPropertyName("id")] public long Id { get; init; }

    [JsonPropertyName("load_number_work_type")]
    public string? LoadNumberWorkType { get; init; }

    [JsonPropertyName("created_at")] public DateTime? CreatedAt { get; init; }

    // İlişkiler aynı adlı sütunları ezer
    [JsonPropertyName("load_status_id")] public LoadStatusDto? LoadStatusId { get; init; }
    [JsonPropertyName("customer_id")] public NamedRefDto? CustomerId { get; init; }
    [JsonPropertyName("sender_id")] public NamedRefDto? SenderId { get; init; }
    [JsonPropertyName("receiver_id")] public NamedRefDto? ReceiverId { get; init; }
    [JsonPropertyName("usercode_with_notification")] public NamedRefDto? AssignedUser { get; init; }
    [JsonPropertyName("work_type")] public NamedRefDto? WorkType { get; init; }
}

public sealed class LoadStatusDto
{
    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("name")] public string? Name { get; init; }
    [JsonPropertyName("load_status_id")] public int? LoadStatusId { get; init; }
    [JsonPropertyName("order_no")] public int? OrderNo { get; init; }
}

public sealed class LinkedExpeditionDto
{
    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("expedition_number")] public string? ExpeditionNumber { get; init; }
    /// <summary>1 = Yükleme, 2 = Boşaltma.</summary>
    [JsonPropertyName("upload_unload")] public int? UploadUnload { get; init; }
    [JsonPropertyName("date")] public DateOnly? Date { get; init; }
    [JsonPropertyName("plate_number")] public string? PlateNumber { get; init; }
}

public sealed class SiberArchiveFileDto
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("name")] public string? Name { get; init; }
    [JsonPropertyName("description")] public string? Description { get; init; }
    [JsonPropertyName("created_at")] public DateTime? CreatedAt { get; init; }
    [JsonPropertyName("created_by")] public string? CreatedBy { get; init; }
    /// <summary>KVKK işareti — arayüz uyarı rozeti gösterir.</summary>
    [JsonPropertyName("personal_data")] public bool PersonalData { get; init; }
    /// <summary>Doluysa Siber'de yalnızca bu gruplara açık.</summary>
    [JsonPropertyName("restricted_groups")] public string? RestrictedGroups { get; init; }
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
    [JsonPropertyName("total_lademeter_m3")] public decimal? TotalLademeterM3 { get; init; }
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
    /// DÜRÜST NOT: bu 8 alan (romork_type_id .. final_transportation_by_us)
    /// önceden bu DTO'da HİÇ YOKTU — `LoadTransferUpdateRequest`'te (yazma)
    /// zaten karşılığı olmasına rağmen okuma tarafı boştu. Sonucu: formu
    /// AÇIP dokunmadan Kaydet'e basmak bu alanları SESSİZCE boşaltıyordu
    /// (frontend `undefined`'ı boş gönderiyordu). Bu güncellemede eklendi.
    /// </summary>
    [JsonPropertyName("romork_type_id")] public NamedRefDto? RomorkTypeId { get; init; }
    [JsonPropertyName("instruction_id")] public NamedRefDto? InstructionId { get; init; }
    [JsonPropertyName("delivery_method_id")] public NamedRefDto? DeliveryMethodId { get; init; }
    [JsonPropertyName("load_transfer_type_id")] public NamedRefDto? LoadTransferTypeId { get; init; }
    [JsonPropertyName("way_of_working")] public int? WayOfWorking { get; init; }
    [JsonPropertyName("front_transportation_by_us")] public int? FrontTransportationByUs { get; init; }
    [JsonPropertyName("final_transportation_by_us")] public int? FinalTransportationByUs { get; init; }
    [JsonPropertyName("departure_country_id")] public CountryDto? DepartureCountryId { get; init; }
    [JsonPropertyName("target_country_id")] public CountryDto? TargetCountryId { get; init; }

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

    /// <summary>
    /// Siber'in FTP arşivindeki evraklar (sbr_arsiv). Yerel yüklenen dosyalardan
    /// (load_file) AYRI tutulur: bunlar Siber programından eklenmiş, sahibi Siber
    /// olan belgelerdir — buradan silinemez/düzenlenemez.
    /// </summary>
    [JsonPropertyName("siber_archive")] public IReadOnlyList<SiberArchiveFileDto> SiberArchive { get; init; } = [];

    /// <summary>
    /// Bu yükün bağlı olduğu sefer(ler).
    ///
    /// LİSTE, tek değer değil: "bir yük yalnızca bir sefere bağlanır" kuralı
    /// canlı veride tutmuyor — 7.686 yükün 143'ü birden fazla sefere bağlı ve bu
    /// her yıl tekrarlıyor (2026'da 12). Kaynak tablonun adı da bunu açıklıyor:
    /// skn_yukaktarma, yani yükün seferler arasında AKTARILMASI. Tek alan olarak
    /// modellenirse bu yüklerde ikinci sefer sessizce kaybolurdu.
    /// </summary>
    [JsonPropertyName("expeditions")] public IReadOnlyList<LinkedExpeditionDto> Expeditions { get; init; } = [];

    [JsonPropertyName("invoices")] public IReadOnlyList<LoadTransferInvoiceDto> Invoices { get; init; } = [];

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
    [JsonPropertyName("case_type_id")] public NamedRefDto? CaseTypeId { get; init; }
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
    [JsonPropertyName("item_id")] public FinancialItemRefDto? ItemId { get; init; }
    [JsonPropertyName("account_id")] public NamedRefDto? AccountId { get; init; }
    [JsonPropertyName("currency_code")] public CurrencyDto? CurrencyCode { get; init; }
}

/// <summary>
/// olsold: <c>LoadFormInvoices.vue</c> — bu Yük'ün fatura kalemlerinin
/// eşlendiği gerçek Fatura kayıtları (salt-okunur çapraz görünüm).
///
/// DÜRÜST NOT: KDV/tutar alanları önceden "Uyumsoft'a bağlı, bu portta hiç
/// yok" gerekçesiyle bilinçli olarak dışarıda bırakılmıştı — bu YANLIŞTI.
/// <c>Invoice</c> entity'sinde bu sütunlar (PayableAmount/TaxAmount/
/// TaxExclusiveAmount/TaxRate/DocumentCurrencyCode/CommercialType) zaten var
/// ve ana Fatura modülü (<c>InvoiceService</c>/<c>InvoicesPage.tsx</c>) bunları
/// ZATEN okuyup gösteriyor — yalnızca bu çapraz görünümde unutulmuşlar.
/// Uyumsoft entegrasyonu olmadığı için değerleri genelde null/0 olacak, ama
/// kaynak da (<c>useMoneyFormat</c>) null'u sahte "0,00" ile gösteriyor —
/// bu davranış birebir korunuyor, alan gizlenmiyor.
/// </summary>
public sealed class LoadTransferInvoiceDto
{
    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("invoice_id")] public string? InvoiceId { get; init; }
    [JsonPropertyName("box_type")] public short BoxType { get; init; }
    [JsonPropertyName("commercial_type")] public int CommercialType { get; init; }
    [JsonPropertyName("target_title")] public string? TargetTitle { get; init; }
    [JsonPropertyName("target_identity_no")] public string? TargetIdentityNo { get; init; }
    [JsonPropertyName("invoice_execution_date")] public DateTime? InvoiceExecutionDate { get; init; }
    [JsonPropertyName("invoice_status")] public NamedRefDto? InvoiceStatus { get; init; }
    [JsonPropertyName("invoice_type")] public NamedRefDto? InvoiceType { get; init; }
    [JsonPropertyName("payable_amount")] public decimal? PayableAmount { get; init; }
    [JsonPropertyName("tax_exclusive_amount")] public decimal? TaxExclusiveAmount { get; init; }
    [JsonPropertyName("tax_amount")] public decimal? TaxAmount { get; init; }
    [JsonPropertyName("tax_rate")] public decimal? TaxRate { get; init; }
    [JsonPropertyName("document_currency_code")] public string? DocumentCurrencyCode { get; init; }
}

public sealed class LoadTransferService : ILoadTransferService
{
    private readonly OlsDbContext _db;

    private readonly ISiberArchiveRepository _archive;
    private readonly ICompanyScope _companyScope;
    private readonly ICurrentUser _currentUser;

    public LoadTransferService(
        OlsDbContext db, ISiberArchiveRepository archive,
        ICompanyScope companyScope, ICurrentUser currentUser)
    {
        _db = db;
        _archive = archive;
        _companyScope = companyScope;
        _currentUser = currentUser;
    }

    public async Task<object> ListAsync(
        LoadTransferListQuery query, CancellationToken cancellationToken = default)
    {
        var transfers = _db.LoadTransfers.AsNoTracking();

        // ŞİRKET GÖRÜNÜRLÜĞÜ (AVRORA / OLS). Filtre listede uygulanır ki Avrora
        // kayıtları yetkisiz kullanıcının listesinde HİÇ görünmesin; detay ucu da
        // ayrıca korunur (bkz. SingleAsync) — aksi hâlde id tahmin edilerek
        // doğrudan erişilebilirdi.
        var visibility = await _companyScope.ResolveAsync(_currentUser.Id, cancellationToken);

        if (!visibility.SeesEverything)
        {
            transfers = visibility.OnlyCompanyId is { } only
                ? transfers.Where(t => t.SiberCompanyId == only)
                : transfers.Where(t => t.SiberCompanyId == null ||
                                       t.SiberCompanyId != visibility.ExcludeCompanyId);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            // Arama kutusu "Yük no, müşteri..." yazıyor ama önceden sadece yük
            // numarasını tarıyordu - müşteri/gönderici/alıcı/görevli/durum/kap
            // tipi/mali kalem adına ve kiloya göre de aranabilsin diye genişletildi.
            // Türkçe noktasız I/ı normalizasyonu için bkz. QueryableExtensions.NormalizeTurkish.
            var pattern = $"%{QueryableExtensions.NormalizeTurkish(query.Search)}%";

            // LoadTransferPackage.CaseTypeId yerel case_types.id'sini METİN olarak
            // tutuyor (bkz. LoadTransferPackagesAsync yorumu) - EF Core string->int
            // karşılaştırmasını SQL'e çeviremiyor, o yüzden eşleşen id'leri önce
            // küçük bir sorguyla metne çevirip çekiyoruz.
            var matchingCaseTypeIds = await _db.CaseTypes
                .Where(c => EF.Functions.Like(c.Name!.Replace("İ", "i").Replace("I", "i").Replace("ı", "i").ToLower(), pattern))
                .Select(c => c.Id.ToString())
                .ToListAsync(cancellationToken);

            var matchingFinancialItemIds = await _db.FinancialItems
                .Where(f => EF.Functions.Like(f.Name!.Replace("İ", "i").Replace("I", "i").Replace("ı", "i").ToLower(), pattern))
                .Select(f => (int)f.Id)
                .ToListAsync(cancellationToken);

            // DÜRÜST NOT / performans: bu 3 alan önceden _db.Accounts.Any(...) ile
            // korelasyonlu EXISTS olarak yazılıyordu - Postgres bunu 8 OR'lu koşulun
            // TAMAMI birlikteyken (parametreli/hazırlanmış sorgu olarak) bazen doğru
            // plana çeviremiyor ve saniyeler yerine 30+ SANİYE sürebiliyordu (canlı
            // ölçüldü). Diğer alt sorgular gibi ÖNCEDEN materialize edilen (ToListAsync)
            // küçük id listelerine çevrildi - Contains() her zaman güvenilir/hızlı
            // (= ANY(@array)) çeviriliyor, sorgu planlayıcısının 8'li OR'u nasıl
            // ele alacağına bağlı kalınmıyor.
            var matchingAccountIds = await _db.Accounts
                .Where(a => EF.Functions.Like(a.Name!.Replace("İ", "i").Replace("I", "i").Replace("ı", "i").ToLower(), pattern))
                .Select(a => (int)a.Id)
                .ToListAsync(cancellationToken);

            var matchingUserIds = await _db.Users
                .Where(u => EF.Functions.Like(((u.Name ?? "") + " " + (u.Surname ?? "")).Replace("İ", "i").Replace("I", "i").Replace("ı", "i").ToLower(), pattern))
                .Select(u => (int)u.Id)
                .ToListAsync(cancellationToken);

            var matchingStatusIds = await _db.LoadStatusTypes
                .Where(s => EF.Functions.Like(s.Name!.Replace("İ", "i").Replace("I", "i").Replace("ı", "i").ToLower(), pattern))
                .Select(s => (int)s.Id)
                .ToListAsync(cancellationToken);

            var searchWeight =
                decimal.TryParse(query.Search, NumberStyles.Number, CultureInfo.InvariantCulture, out var w1) ? w1
                : decimal.TryParse(query.Search, NumberStyles.Number, new CultureInfo("tr-TR"), out var w2) ? w2
                : (decimal?)null;

            // Aynı performans nedeniyle (yukarıdaki not) bu ikisi de korelasyonlu
            // Any() yerine önceden çekilen eşleşen LoadTransferId/InsertName
            // kümelerine çevrildi.
            var matchingPackageTransferIds = await _db.LoadTransferPackages
                .Where(p => matchingCaseTypeIds.Contains(p.CaseTypeId!) ||
                            (searchWeight != null && (p.GrossWeight == searchWeight || p.NetWeight == searchWeight)))
                .Select(p => p.LoadTransferId)
                .Distinct()
                .ToListAsync(cancellationToken);

            var matchingInvoiceItemInsertNames = await _db.LoadTransferInvoiceItems
                .Where(i => i.ItemId != null && matchingFinancialItemIds.Contains(i.ItemId.Value))
                .Select(i => i.InsertName)
                .Distinct()
                .ToListAsync(cancellationToken);

            transfers = transfers.Where(t =>
                EF.Functions.Like(t.LoadNumberWorkType!.Replace("İ", "i").Replace("I", "i").Replace("ı", "i").ToLower(), pattern) ||
                (t.CustomerId != null && matchingAccountIds.Contains(t.CustomerId.Value)) ||
                (t.SenderId != null && matchingAccountIds.Contains(t.SenderId.Value)) ||
                (t.ReceiverId != null && matchingAccountIds.Contains(t.ReceiverId.Value)) ||
                (t.UsercodeWithNotification != null && matchingUserIds.Contains(t.UsercodeWithNotification.Value)) ||
                (t.LoadStatusId != null && matchingStatusIds.Contains(t.LoadStatusId.Value)) ||
                matchingPackageTransferIds.Contains(t.LoadTransferId) ||
                matchingInvoiceItemInsertNames.Contains(t.LoadNumberWorkType));
        }

        // Detaylı arama bölümü: aşağıdakiler VE (AND) mantığıyla birleşir - genel
        // arama kutusunun aksine her doldurulan alan sonucu daha da daraltır.
        // Müşteri/Gönderici/Alıcı serbest metin değil, AccountPicker'dan seçilen
        // gerçek cari id'sidir (kayıtlı Siber verisinden) - tam eşleşme.
        if (query.CustomerId is { } customerId)
            transfers = transfers.Where(t => t.CustomerId == customerId);

        if (query.SenderId is { } senderId)
            transfers = transfers.Where(t => t.SenderId == senderId);

        if (query.ReceiverId is { } receiverId)
            transfers = transfers.Where(t => t.ReceiverId == receiverId);

        if (query.AssignedUserId is { } assignedUserId)
            transfers = transfers.Where(t => t.UsercodeWithNotification == assignedUserId);

        if (!string.IsNullOrWhiteSpace(query.FinancialItem))
        {
            var p = $"%{QueryableExtensions.NormalizeTurkish(query.FinancialItem)}%";
            var financialItemIds = await _db.FinancialItems
                .Where(f => EF.Functions.Like(f.Name!.Replace("İ", "i").Replace("I", "i").Replace("ı", "i").ToLower(), p))
                .Select(f => (int)f.Id)
                .ToListAsync(cancellationToken);
            transfers = transfers.Where(t => _db.LoadTransferInvoiceItems.Any(i =>
                i.InsertName == t.LoadNumberWorkType && i.ItemId != null && financialItemIds.Contains(i.ItemId.Value)));
        }

        if (query.StatusId is { } statusId)
            transfers = transfers.Where(t => t.LoadStatusId == statusId);

        if (query.CaseTypeId is { } caseTypeId)
        {
            var caseTypeIdText = caseTypeId.ToString();
            transfers = transfers.Where(t => _db.LoadTransferPackages.Any(p =>
                p.LoadTransferId == t.LoadTransferId && p.CaseTypeId == caseTypeIdText));
        }

        if (query.Weight is { } weight)
            transfers = transfers.Where(t => _db.LoadTransferPackages.Any(p =>
                p.LoadTransferId == t.LoadTransferId && (p.GrossWeight == weight || p.NetWeight == weight)));

        if (query.WorkTypeId is { } workTypeId)
            transfers = transfers.Where(t => t.WorkType == workTypeId);

        if (query.DateFrom is { } dateFrom)
        {
            var from = dateFrom.ToDateTime(TimeOnly.MinValue);
            transfers = transfers.Where(t => t.CreatedAt >= from);
        }

        if (query.DateTo is { } dateTo)
        {
            var to = dateTo.AddDays(1).ToDateTime(TimeOnly.MinValue);
            transfers = transfers.Where(t => t.CreatedAt < to);
        }

        var projected = transfers
            .OrderByDescending(t => t.CreatedAt)
            .ThenByDescending(t => t.Id)
            .Select(t => new LoadTransferListItemDto
            {
                Id = t.Id,
                LoadNumberWorkType = t.LoadNumberWorkType,
                CreatedAt = t.CreatedAt,
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
                ReceiverId = _db.Accounts.Where(a => a.Id == t.ReceiverId)
                    .Select(a => new NamedRefDto { Id = a.Id, Name = a.Name })
                    .FirstOrDefault(),
                AssignedUser = _db.Users.Where(u => u.Id == t.UsercodeWithNotification)
                    .Select(u => new NamedRefDto { Id = u.Id, Name = u.Name + " " + u.Surname })
                    .FirstOrDefault(),
                WorkType = _db.WorkTypes.Where(w => w.Id == t.WorkType)
                    .Select(w => new NamedRefDto { Id = w.Id, Name = w.Name, Code = w.Code, SiberId = w.SiberId })
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

        // Detay da filtrelenir: liste gizlese bile id ile doğrudan istenebilirdi.
        var visibility = await _companyScope.ResolveAsync(_currentUser.Id, cancellationToken);
        if (!visibility.Allows(t.SiberCompanyId))
            return null;

        // Eşleme tablosu yerel id'yi metin olarak tutuyor.
        var transferIdText = t.Id.ToString();

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
            TotalLademeterM3 = t.TotalLademeterM3,
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

            RomorkTypeId = await _db.RomorkTypes.AsNoTracking()
                .Where(r => r.Id == t.RomorkTypeId)
                .Select(r => new NamedRefDto { Id = r.Id, Name = r.Name, Code = r.Code })
                .FirstOrDefaultAsync(cancellationToken),

            InstructionId = await _db.Instructions.AsNoTracking()
                .Where(i => i.Id == t.InstructionId)
                .Select(i => new NamedRefDto { Id = i.Id, Name = i.Name, Code = i.Code })
                .FirstOrDefaultAsync(cancellationToken),

            DeliveryMethodId = await _db.LoadTransferDeliveryMethods.AsNoTracking()
                .Where(m => m.Id == t.DeliveryMethodId)
                .Select(m => new NamedRefDto { Id = m.Id, Name = m.Name })
                .FirstOrDefaultAsync(cancellationToken),

            LoadTransferTypeId = await _db.LoadTransferTypes.AsNoTracking()
                .Where(l => l.Id == t.LoadTransferTypeId)
                .Select(l => new NamedRefDto { Id = l.Id, Name = l.Name, Code = l.Code })
                .FirstOrDefaultAsync(cancellationToken),

            WayOfWorking = t.WayOfWorking,
            FrontTransportationByUs = t.FrontTransportationByUs,
            FinalTransportationByUs = t.FinalTransportationByUs,

            DepartureCountryId = await CountryRefAsync(t.DepartureCountryId, cancellationToken),
            TargetCountryId = await CountryRefAsync(t.TargetCountryId, cancellationToken),

            CustomerRepresentative = await UserRefAsync(t.CustomerRepresentativeName, cancellationToken),
            SecondCustomerRepresentative = await UserRefAsync(t.SecondCustomerRepresentativeName, cancellationToken),

            OriginalLoadId = originalLoadId,

            // Sefer bağı expedition_load_mappings üzerinden; eşleme sütunu yerel
            // sayısal id'yi METİN olarak tutuyor (bkz. ExpeditionLoadMappingService).
            Expeditions = await (
                from m in _db.ExpeditionLoadMappings.AsNoTracking()
                where m.LoadTransferId == transferIdText
                join e in _db.Expeditions.AsNoTracking()
                    on m.ExpeditionId equals e.Id.ToString()
                select new LinkedExpeditionDto
                {
                    Id = e.Id,
                    ExpeditionNumber = e.ExpeditionNumber,
                    UploadUnload = m.UploadUnload,
                    Date = m.Date,
                    PlateNumber = _db.Cars.Where(c => c.Id == e.RomorkId)
                        .Select(c => c.PlateNumber).FirstOrDefault(),
                })
                .Distinct()
                .ToListAsync(cancellationToken),

            // Siber arşivi: yükün Siber kimliğiyle (skn_yuk.yukid) bağlanır.
            // Bağlantı yapılandırılmamışsa boş liste döner, ekran bozulmaz.
            SiberArchive = (await _archive.ListByModuleAsync(t.LoadTransferId ?? string.Empty, cancellationToken))
                .Select(a => new SiberArchiveFileDto
                {
                    Id = a.ArsivId,
                    Name = a.Ad,
                    Description = a.Aciklama,
                    CreatedAt = a.KayitGirisTarih,
                    CreatedBy = a.KayitGiren,
                    PersonalData = a.KisiselVeri,
                    RestrictedGroups = string.IsNullOrWhiteSpace(a.YetkiliGruplar) ? null : a.YetkiliGruplar,
                })
                .ToList(),

            // Dosyalar teklife VEYA doğrudan yüke bağlı olabilir (teklifsiz yükler).
            LoadFile = await _db.LoadFiles.AsNoTracking()
                    .Where(f => (originalLoadId != null && f.LoadId == (int)originalLoadId) ||
                                f.LoadTransferId == t.Id)
                    .Select(f => new LoadFileDto
                    {
                        Id = f.Id, LoadId = f.LoadId, File = f.File,
                        MimeType = f.MimeType, OrgName = f.OrgName, CreatedAt = f.CreatedAt,
                    })
                    .ToListAsync(cancellationToken),

            Invoices = await _db.LoadTransferInvoiceMaps.AsNoTracking()
                .Where(m => m.LoadTransferId == t.Id)
                .Select(m => m.Invoice)
                .Distinct()
                .Select(i => new LoadTransferInvoiceDto
                {
                    Id = i.Id,
                    InvoiceId = i.InvoiceId,
                    BoxType = i.BoxType,
                    CommercialType = i.CommercialType,
                    TargetTitle = i.TargetTitle,
                    TargetIdentityNo = i.TargetIdentityNo,
                    PayableAmount = i.PayableAmount,
                    TaxExclusiveAmount = i.TaxExclusiveAmount,
                    TaxAmount = i.TaxAmount,
                    TaxRate = i.TaxRate,
                    DocumentCurrencyCode = i.DocumentCurrencyCode,
                    InvoiceExecutionDate = i.InvoiceExecutionDate,
                    InvoiceStatus = i.InvoiceStatus == null ? null
                        : new NamedRefDto { Id = i.InvoiceStatus.Id, Name = i.InvoiceStatus.Name },
                    InvoiceType = i.InvoiceType == null ? null
                        : new NamedRefDto { Id = i.InvoiceType.Id, Name = i.InvoiceType.Name },
                })
                .ToListAsync(cancellationToken),

            // Koli ve fatura kalemleri Siber kimliği üzerinden bağlanır
            // (load_transfers.load_transfer_id metin sütunu).
            LoadTransferPackage = await LoadTransferPackagesAsync(t.LoadTransferId, cancellationToken),

            LoadTransferInvoiceItem = await _db.LoadTransferInvoiceItems.AsNoTracking()
                .Where(i => i.InsertName == t.LoadNumberWorkType)
                .Select(i => new LoadTransferInvoiceItemDto
                {
                    Id = i.Id, Modulkalemid = i.Modulkalemid, Buysell = i.Buysell,
                    NetPrice = i.NetPrice, TotalPrice = i.TotalPrice, Quantity = i.Quantity,
                    Description = i.Description, Status = i.Status,
                    ItemId = _db.FinancialItems.Where(f => f.Id == i.ItemId)
                        .Select(f => new FinancialItemRefDto { Id = f.Id, Name = f.Name, Type = f.Type ?? 0 })
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

    /// <summary>
    /// <c>LoadTransferPackage.CaseTypeId</c> (aksine <c>ProductTypeId</c>'ye)
    /// <c>string?</c> — dönüşüm kodu <c>ToString()</c> ile yazıyor (bkz.
    /// LoadTransferWriteService.WritePackagesAsync). EF Core string->int
    /// karşılaştırmasını SQL'e çeviremediği için CaseType eşlemesi bellekte yapılır.
    /// </summary>
    private async Task<IReadOnlyList<LoadTransferPackageDto>> LoadTransferPackagesAsync(
        string? loadTransferId, CancellationToken cancellationToken)
    {
        var packages = await _db.LoadTransferPackages.AsNoTracking()
            .Where(p => p.LoadTransferId == loadTransferId)
            .ToListAsync(cancellationToken);

        var productTypeIds = packages.Where(p => p.ProductTypeId != null).Select(p => p.ProductTypeId!.Value).Distinct().ToList();
        var productTypes = await _db.ProductTypes.AsNoTracking()
            .Where(x => productTypeIds.Contains((int)x.Id))
            .ToDictionaryAsync(x => (int)x.Id, x => new NamedRefDto { Id = x.Id, Name = x.Name }, cancellationToken);

        var caseTypeIds = packages
            .Select(p => int.TryParse(p.CaseTypeId, out var cid) ? cid : (int?)null)
            .Where(cid => cid != null)
            .Select(cid => cid!.Value)
            .Distinct()
            .ToList();
        var caseTypes = await _db.CaseTypes.AsNoTracking()
            .Where(x => caseTypeIds.Contains((int)x.Id))
            .ToDictionaryAsync(x => (int)x.Id, x => new NamedRefDto { Id = x.Id, Name = x.Name }, cancellationToken);

        return packages.Select(p => new LoadTransferPackageDto
        {
            Id = p.Id, Yukkoliid = p.Yukkoliid, Quantity = p.Quantity,
            GrossWeight = p.GrossWeight, NetWeight = p.NetWeight, Volume = p.Volume,
            Lademeter = p.Lademeter, Width = p.Width, Length = p.Length,
            Height = p.Height, Stackable = p.Stackable,
            ProductTypeId = p.ProductTypeId is { } pid && productTypes.TryGetValue(pid, out var pt) ? pt : null,
            CaseTypeId = int.TryParse(p.CaseTypeId, out var cid) && caseTypes.TryGetValue(cid, out var ct) ? ct : null,
        }).ToList();
    }

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

    /// <summary>
    /// <c>LoadTransfer.DepartureCountryId</c>/<c>TargetCountryId</c> Load'un aksine
    /// <c>string?</c> — dönüşüm anında Guid'in <c>.ToString()</c>'u yazılır (bkz.
    /// LoadTransferWriteService.ConvertOfferAsync), AMA BULUNAN GERÇEK BUG: olsold'un
    /// kendi <c>update()</c> akışı (LoadTransferController.php satır 719-720)
    /// Siber'in <c>_yuklemeulke</c>/<c>_bosaltmaulke</c> sütununa GUID DEĞİL, ÜLKE
    /// ADINI yazıyordu (<c>$load_transfer->departureCountryId->name</c>) — ve gerçek
    /// Siber'de bu kural neredeyse tüm satırlarda geçerli (canlıda doğrulandı: 11046
    /// satırın yalnızca 411'i GUID, 10631'i düz ülke adı). Salt GUID araması bu
    /// yüzden Kalkış/Varış Ülkesi'ni satırların %96'sında boş gösteriyordu. Burada da
    /// aynı iki biçim (GUID ÖNCE, olmazsa isim) desteklenir.
    /// </summary>
    private async Task<CountryDto?> CountryRefAsync(string? countryId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(countryId))
            return null;

        if (Guid.TryParse(countryId, out var id))
        {
            var byId = await _db.Countries.AsNoTracking()
                .Where(c => c.Id == id)
                .Select(c => new CountryDto { Id = c.Id, Name = c.Name })
                .FirstOrDefaultAsync(cancellationToken);
            if (byId is not null)
                return byId;
        }

        return await _db.Countries.AsNoTracking()
            .Where(c => c.Name != null && EF.Functions.ILike(c.Name, countryId))
            .Select(c => new CountryDto { Id = c.Id, Name = c.Name })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
