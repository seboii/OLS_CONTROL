using System.Text.Json.Serialization;
using OLS.Business.Common;
using OLS.Business.Services.Accounts;

namespace OLS.Business.Services.Loads;

/// <summary>
/// Yük/teklif yanıt şekilleri.
///
/// Cari modülündeki aynı Eloquent kuralı burada da geçerli: yüklenen ilişki
/// aynı adlı sütunu EZER. Örneğin <c>customer_id</c> tam sayı değil cari nesnesidir
/// (frontend <c>data.customer_id.name</c> ve <c>data.customer_id.country_id</c> okuyor).
/// Ayrıntı için <see cref="AccountListItemDto"/> üstündeki nota bakın.
/// </summary>
public sealed class LoadListItemDto
{
    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("reservation_number")] public string? ReservationNumber { get; init; }
    [JsonPropertyName("load_number")] public string? LoadNumber { get; init; }
    [JsonPropertyName("offer_date")] public DateOnly? OfferDate { get; init; }
    /// <summary>Olumlu'ya çekilme günü — sunucu damgalar, arayüz salt-okunur gösterir.</summary>
    [JsonPropertyName("approval_date")] public DateOnly? ApprovalDate { get; init; }
    [JsonPropertyName("offer_validity_date")] public DateOnly? OfferValidityDate { get; init; }
    [JsonPropertyName("marketing_notification_date")] public DateOnly? MarketingNotificationDate { get; init; }
    [JsonPropertyName("description")] public string? Description { get; init; }
    /// <summary>Teklif Olumsuz ise gerekcesi — bkz. Load.RejectionReason.</summary>
    [JsonPropertyName("rejection_reason")] public string? RejectionReason { get; init; }
    [JsonPropertyName("payer_company")] public string? PayerCompany { get; init; }
    [JsonPropertyName("front_transportation_by_us")] public int FrontTransportationByUs { get; init; }
    [JsonPropertyName("final_transportation_by_us")] public int FinalTransportationByUs { get; init; }
    [JsonPropertyName("way_of_working")] public int WayOfWorking { get; init; }
    [JsonPropertyName("transfer_to_siber")] public int TransferToSiber { get; init; }
    [JsonPropertyName("siber_id")] public string? SiberId { get; init; }
    [JsonPropertyName("status_type_id")] public int? StatusTypeIdRaw { get; init; }
    [JsonPropertyName("created_at")] public DateTime? CreatedAt { get; init; }
    [JsonPropertyName("updated_at")] public DateTime? UpdatedAt { get; init; }

    // withCount('loadContent') karşılığı
    [JsonPropertyName("load_content_count")] public int LoadContentCount { get; init; }

    // İlişkiler — aynı adlı sütunları ezerler
    [JsonPropertyName("work_type_id")] public NamedRefDto? WorkTypeId { get; init; }
    [JsonPropertyName("loading_type_id")] public NamedRefDto? LoadingTypeId { get; init; }
    [JsonPropertyName("load_transfer_type_id")] public NamedRefDto? LoadTransferTypeId { get; init; }
    [JsonPropertyName("instruction_id")] public NamedRefDto? InstructionId { get; init; }
    [JsonPropertyName("romork_type_id")] public NamedRefDto? RomorkTypeId { get; init; }
    [JsonPropertyName("department_id")] public NamedRefDto? DepartmentId { get; init; }

    [JsonPropertyName("customer_id")] public AccountRefDto? CustomerId { get; init; }
    [JsonPropertyName("sender_id")] public AccountRefDto? SenderId { get; init; }
    [JsonPropertyName("receiver_id")] public AccountRefDto? ReceiverId { get; init; }
    [JsonPropertyName("agent_id")] public AccountRefDto? AgentId { get; init; }
    [JsonPropertyName("company_pay_freight_id")] public AccountRefDto? CompanyPayFreightId { get; init; }

    [JsonPropertyName("load_charge_person")]
    public IReadOnlyList<LoadChargePersonDto> LoadChargePerson { get; init; } = [];
}

/// <summary>single() yanıtı: liste alanları + tüm alt kayıtlar ve ek ilişkiler.</summary>
public sealed class LoadArchiveDto
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("name")] public string? Name { get; init; }
    [JsonPropertyName("created_at")] public DateTime? CreatedAt { get; init; }
    [JsonPropertyName("created_by")] public string? CreatedBy { get; init; }
    [JsonPropertyName("personal_data")] public bool PersonalData { get; init; }
    [JsonPropertyName("restricted_groups")] public string? RestrictedGroups { get; init; }
}

public sealed class LoadDetailDto
{
    /// <summary>Siber izleri — kim açtı, kim son dokundu, silindi mi.</summary>
    [JsonPropertyName("siber_audit")] public SiberAuditDto? SiberAudit { get; init; }

    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("reservation_number")] public string? ReservationNumber { get; init; }
    [JsonPropertyName("load_number")] public string? LoadNumber { get; init; }
    [JsonPropertyName("offer_date")] public DateOnly? OfferDate { get; init; }
    /// <summary>Olumlu'ya çekilme günü — sunucu damgalar, arayüz salt-okunur gösterir.</summary>
    [JsonPropertyName("approval_date")] public DateOnly? ApprovalDate { get; init; }
    [JsonPropertyName("offer_validity_date")] public DateOnly? OfferValidityDate { get; init; }
    [JsonPropertyName("marketing_notification_date")] public DateOnly? MarketingNotificationDate { get; init; }
    [JsonPropertyName("description")] public string? Description { get; init; }
    /// <summary>Teklif Olumsuz ise gerekcesi — bkz. Load.RejectionReason.</summary>
    [JsonPropertyName("rejection_reason")] public string? RejectionReason { get; init; }
    [JsonPropertyName("payer_company")] public string? PayerCompany { get; init; }
    [JsonPropertyName("front_transportation_by_us")] public int FrontTransportationByUs { get; init; }
    [JsonPropertyName("final_transportation_by_us")] public int FinalTransportationByUs { get; init; }
    [JsonPropertyName("way_of_working")] public int WayOfWorking { get; init; }
    [JsonPropertyName("transfer_to_siber")] public int TransferToSiber { get; init; }
    [JsonPropertyName("siber_id")] public string? SiberId { get; init; }

    /// <summary>
    /// Teklifin Siber arşivindeki evrakları (sbr_arsiv.modulid = rezervasyonid).
    /// Yük ve seferdeki karşılıklarıyla aynı yapı.
    /// </summary>
    [JsonPropertyName("siber_archive")] public IReadOnlyList<LoadArchiveDto> SiberArchive { get; init; } = [];
    [JsonPropertyName("mail_id")] public string? MailId { get; init; }
    [JsonPropertyName("created_at")] public DateTime? CreatedAt { get; init; }
    [JsonPropertyName("updated_at")] public DateTime? UpdatedAt { get; init; }

    [JsonPropertyName("work_type_id")] public NamedRefDto? WorkTypeId { get; init; }
    [JsonPropertyName("loading_type_id")] public NamedRefDto? LoadingTypeId { get; init; }
    [JsonPropertyName("payment_type_id")] public NamedRefDto? PaymentTypeId { get; init; }
    [JsonPropertyName("status_type_id")] public NamedRefDto? StatusTypeId { get; init; }
    [JsonPropertyName("load_transfer_type_id")] public NamedRefDto? LoadTransferTypeId { get; init; }
    [JsonPropertyName("instruction_id")] public NamedRefDto? InstructionId { get; init; }
    [JsonPropertyName("romork_type_id")] public NamedRefDto? RomorkTypeId { get; init; }
    [JsonPropertyName("department_id")] public NamedRefDto? DepartmentId { get; init; }

    [JsonPropertyName("customer_id")] public AccountRefDto? CustomerId { get; init; }
    [JsonPropertyName("sender_id")] public AccountRefDto? SenderId { get; init; }
    [JsonPropertyName("receiver_id")] public AccountRefDto? ReceiverId { get; init; }
    [JsonPropertyName("agent_id")] public AccountRefDto? AgentId { get; init; }
    [JsonPropertyName("company_pay_freight_id")] public AccountRefDto? CompanyPayFreightId { get; init; }
    [JsonPropertyName("payer_company_id")] public AccountRefDto? PayerCompanyId { get; init; }

    [JsonPropertyName("departure_country_id")] public CountryDto? DepartureCountryId { get; init; }
    [JsonPropertyName("transit_country_id")] public CountryDto? TransitCountryId { get; init; }
    [JsonPropertyName("target_country_id")] public CountryDto? TargetCountryId { get; init; }

    [JsonPropertyName("load_charge_person")]
    public IReadOnlyList<LoadChargePersonDto> LoadChargePerson { get; init; } = [];

    [JsonPropertyName("load_content")]
    public IReadOnlyList<LoadContentDto> LoadContent { get; init; } = [];

    [JsonPropertyName("load_financial_item")]
    public IReadOnlyList<LoadFinancialItemDto> LoadFinancialItem { get; init; } = [];

    [JsonPropertyName("load_movement")]
    public IReadOnlyList<LoadMovementDto> LoadMovement { get; init; } = [];

    [JsonPropertyName("load_file")]
    public IReadOnlyList<LoadFileDto> LoadFile { get; init; } = [];

    /// <summary>olsold: "E-Posta Ayarları" sekmesi (offer_data.email.to/.cc).</summary>
    [JsonPropertyName("email_to")]
    public IReadOnlyList<string> EmailTo { get; init; } = [];

    [JsonPropertyName("email_cc")]
    public IReadOnlyList<string> EmailCc { get; init; } = [];
}

/// <summary>id + name taşıyan basit referans tabloları (iş tipi, departman vb.).</summary>
public sealed class NamedRefDto
{
    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("name")] public string? Name { get; init; }
    [JsonPropertyName("code")] public string? Code { get; init; }
    [JsonPropertyName("siber_id")] public string? SiberId { get; init; }
}

/// <summary>
/// financial_items referansı — NamedRefDto'ya ek olarak Type taşır (bit maskesi:
/// 1=Alış/Gider, 2=Satış/Gelir, 3=ikisi de). Frontend Kalem seçildiğinde Alış/Satış
/// alanını buradan otomatik belirliyor (bkz. FinancialItemPicker.tsx).
/// </summary>
public sealed class FinancialItemRefDto
{
    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("name")] public string? Name { get; init; }
    [JsonPropertyName("type")] public int Type { get; init; }
}

/// <summary>
/// Yük üzerindeki cari referansları (müşteri/gönderici/alıcı/acente).
/// olsold bunları <c>countryId, cityId, districtId</c> ile birlikte yüklüyordu.
/// </summary>
public sealed class AccountRefDto
{
    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("name")] public string? Name { get; init; }
    [JsonPropertyName("avatar")] public string? Avatar { get; init; }
    [JsonPropertyName("email")] public string? Email { get; init; }
    [JsonPropertyName("phone")] public string? Phone { get; init; }
    [JsonPropertyName("address")] public string? Address { get; init; }
    [JsonPropertyName("tax_number")] public string? TaxNumber { get; init; }
    [JsonPropertyName("siber_id")] public string? SiberId { get; init; }

    [JsonPropertyName("country_id")] public CountryDto? CountryId { get; init; }
    [JsonPropertyName("city_id")] public CityDto? CityId { get; init; }
    [JsonPropertyName("district_id")] public DistrictDto? DistrictId { get; init; }
}

public sealed class LoadChargePersonDto
{
    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("load_id")] public int? LoadId { get; init; }
    [JsonPropertyName("user_type")] public int? UserType { get; init; }
    [JsonPropertyName("user_id")] public MappedUserDto? UserId { get; init; }
}

public sealed class LoadContentDto
{
    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("load_id")] public long LoadId { get; init; }
    [JsonPropertyName("quantity")] public int? Quantity { get; init; }
    [JsonPropertyName("gross_weight")] public decimal? GrossWeight { get; init; }
    [JsonPropertyName("net_weight")] public decimal? NetWeight { get; init; }
    [JsonPropertyName("volume")] public decimal? Volume { get; init; }
    [JsonPropertyName("lademeter")] public decimal? Lademeter { get; init; }
    [JsonPropertyName("width")] public decimal? Width { get; init; }
    [JsonPropertyName("length")] public decimal? Length { get; init; }
    [JsonPropertyName("height")] public decimal? Height { get; init; }
    [JsonPropertyName("stackable")] public int? Stackable { get; init; }
    [JsonPropertyName("siber_id")] public string? SiberId { get; init; }
    [JsonPropertyName("product_type_id")] public NamedRefDto? ProductTypeId { get; init; }
    [JsonPropertyName("case_type_id")] public NamedRefDto? CaseTypeId { get; init; }
}

public sealed class LoadFinancialItemDto
{
    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("load_id")] public long LoadId { get; init; }
    [JsonPropertyName("net_price")] public decimal? NetPrice { get; init; }
    [JsonPropertyName("tax_price")] public decimal? TaxPrice { get; init; }
    [JsonPropertyName("total_price")] public decimal? TotalPrice { get; init; }
    [JsonPropertyName("quantity")] public int? Quantity { get; init; }
    [JsonPropertyName("description")] public string? Description { get; init; }
    [JsonPropertyName("buysell")] public int? Buysell { get; init; }
    [JsonPropertyName("status")] public int? Status { get; init; }
    [JsonPropertyName("order")] public int? Order { get; init; }
    [JsonPropertyName("item")] public FinancialItemRefDto? Item { get; init; }

    /// <summary>currency sütunu currencies tablosuna FK; ilişki adı da "currency".</summary>
    [JsonPropertyName("currency")] public CurrencyDto? Currency { get; init; }
    [JsonPropertyName("account_id")] public AccountRefDto? AccountId { get; init; }
    [JsonPropertyName("transport_type_id")] public NamedRefDto? TransportTypeId { get; init; }
    [JsonPropertyName("item_type_id")] public NamedRefDto? ItemTypeId { get; init; }
}

public sealed class CurrencyDto
{
    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("name")] public string? Name { get; init; }
    [JsonPropertyName("code")] public string? Code { get; init; }
}

public sealed class LoadMovementDto
{
    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("load_id")] public long LoadId { get; init; }
    [JsonPropertyName("note")] public string? Note { get; init; }
    [JsonPropertyName("created_at")] public DateTime? CreatedAt { get; init; }
    [JsonPropertyName("movement_type_id")] public NamedRefDto? MovementTypeId { get; init; }
}

public sealed class LoadFileDto
{
    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("load_id")] public int? LoadId { get; init; }
    [JsonPropertyName("file")] public string? File { get; init; }
    [JsonPropertyName("mime_type")] public string? MimeType { get; init; }
    [JsonPropertyName("org_name")] public string? OrgName { get; init; }
    [JsonPropertyName("created_at")] public DateTime? CreatedAt { get; init; }
}
