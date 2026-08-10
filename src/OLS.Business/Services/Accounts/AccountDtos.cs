using System.Text.Json.Serialization;

namespace OLS.Business.Services.Accounts;

/// <summary>
/// Cari yanıt şekilleri.
///
/// ÖNEMLİ — Eloquent'in <c>toArray()</c> davranışı burada birebir taklit ediliyor:
/// <c>array_merge(attributesToArray(), relationsToArray())</c> yapıldığı ve ilişki
/// adları snake_case'e çevrildiği için, YÜKLENEN İLİŞKİ AYNI ADLI SÜTUNUN ÜZERİNE YAZAR.
///
///   countryId ilişkisi  -> "country_id"      (uuid DEĞİL, ülke NESNESİ)
///   phoneCountryId      -> "phone_country_id"
///   taxOffice           -> "tax_office"      (int DEĞİL, vergi dairesi nesnesi)
///   contactLanguage     -> "contact_language"
///
/// Frontend buna bağımlı: AccountTable.vue <c>data.country_id.name</c> ve
/// <c>data.country_id.country_code</c> okuyor. Ama form GÖNDERİRKEN aynı alana
/// uuid yolluyor (AccountFormDrawer.vue). Yani istek ve yanıt şekli farklı.
/// </summary>
public sealed class AccountListItemDto
{
    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("name")] public string? Name { get; init; }
    [JsonPropertyName("phone")] public string? Phone { get; init; }
    [JsonPropertyName("email")] public string? Email { get; init; }
    [JsonPropertyName("avatar")] public string? Avatar { get; init; }

    [JsonPropertyName("country_id")] public CountryDto? CountryId { get; init; }
    [JsonPropertyName("phone_country_id")] public CountryDto? PhoneCountryId { get; init; }
    [JsonPropertyName("tax_office")] public TaxOfficeDto? TaxOffice { get; init; }

    [JsonPropertyName("account_type_mapping_id")]
    public IReadOnlyList<AccountTypeMappingDto> AccountTypeMappingId { get; init; } = [];
}

/// <summary>single() yanıtı: liste alanları + tüm cari detayları ve ek ilişkiler.</summary>
public sealed class AccountDetailDto
{
    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("name")] public string? Name { get; init; }
    [JsonPropertyName("tax_number")] public string? TaxNumber { get; init; }
    [JsonPropertyName("accounting_code")] public string? AccountingCode { get; init; }
    [JsonPropertyName("invoice_type")] public int? InvoiceType { get; init; }
    [JsonPropertyName("address")] public string? Address { get; init; }
    [JsonPropertyName("phone")] public string? Phone { get; init; }
    [JsonPropertyName("avatar")] public string? Avatar { get; init; }
    [JsonPropertyName("email")] public string? Email { get; init; }
    [JsonPropertyName("contact_person")] public string? ContactPerson { get; init; }
    [JsonPropertyName("individual_personal")] public string? IndividualPersonal { get; init; }
    [JsonPropertyName("discount")] public int Discount { get; init; }
    [JsonPropertyName("siber_id")] public string? SiberId { get; init; }
    [JsonPropertyName("created_at")] public DateTime? CreatedAt { get; init; }
    [JsonPropertyName("updated_at")] public DateTime? UpdatedAt { get; init; }

    // İlişkiler — aynı adlı sütunları ezerler (yukarıdaki nota bakın)
    [JsonPropertyName("country_id")] public CountryDto? CountryId { get; init; }
    [JsonPropertyName("city_id")] public CityDto? CityId { get; init; }
    [JsonPropertyName("district_id")] public DistrictDto? DistrictId { get; init; }
    [JsonPropertyName("phone_country_id")] public CountryDto? PhoneCountryId { get; init; }
    [JsonPropertyName("contact_language")] public CountryDto? ContactLanguage { get; init; }
    [JsonPropertyName("tax_office")] public TaxOfficeDto? TaxOffice { get; init; }

    [JsonPropertyName("account_type_mapping_id")]
    public IReadOnlyList<AccountTypeMappingDto> AccountTypeMappingId { get; init; } = [];

    [JsonPropertyName("account_contact_person")]
    public IReadOnlyList<AccountContactPersonDto> AccountContactPerson { get; init; } = [];

    [JsonPropertyName("user_account_mapping")]
    public IReadOnlyList<UserAccountMappingDto> UserAccountMapping { get; init; } = [];
}

public sealed class CountryDto
{
    [JsonPropertyName("id")] public Guid Id { get; init; }
    [JsonPropertyName("name")] public string? Name { get; init; }
    [JsonPropertyName("country_code")] public string? CountryCode { get; init; }
    [JsonPropertyName("flag")] public string? Flag { get; init; }
    [JsonPropertyName("phone_code")] public string? PhoneCode { get; init; }
    [JsonPropertyName("slug")] public string? Slug { get; init; }
}

public sealed class CityDto
{
    [JsonPropertyName("id")] public Guid Id { get; init; }
    [JsonPropertyName("name")] public string? Name { get; init; }
    [JsonPropertyName("country_id")] public string? CountryId { get; init; }
}

public sealed class DistrictDto
{
    [JsonPropertyName("id")] public Guid Id { get; init; }
    [JsonPropertyName("name")] public string? Name { get; init; }
    [JsonPropertyName("city_id")] public string? CityId { get; init; }
}

public sealed class TaxOfficeDto
{
    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("name")] public string? Name { get; init; }
    [JsonPropertyName("city")] public string? City { get; init; }
    [JsonPropertyName("special_code")] public int? SpecialCode { get; init; }
}

/// <summary>
/// account_type_mappings satırı. İçindeki <c>accountTypeId</c> ilişkisi de
/// <c>account_type_id</c> anahtarını ezer (int yerine nesne döner).
/// </summary>
public sealed class AccountTypeMappingDto
{
    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("account_id")] public int? AccountId { get; init; }
    [JsonPropertyName("account_type_id")] public AccountTypeDto? AccountTypeId { get; init; }
    [JsonPropertyName("siber_id")] public string? SiberId { get; init; }
}

public sealed class AccountTypeDto
{
    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("siber_id")] public string? SiberId { get; init; }
}

public sealed class AccountContactPersonDto
{
    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("account_id")] public int? AccountId { get; init; }
    [JsonPropertyName("name")] public string? Name { get; init; }
    [JsonPropertyName("email")] public string? Email { get; init; }
}

/// <summary>userAccountMapping ilişkisi; içinde userId -> "user_id" nesnesi.</summary>
public sealed class UserAccountMappingDto
{
    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("account_id")] public int AccountId { get; init; }
    [JsonPropertyName("user_id")] public MappedUserDto? UserId { get; init; }
}

public sealed class MappedUserDto
{
    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("surname")] public string Surname { get; init; } = string.Empty;
    [JsonPropertyName("email")] public string Email { get; init; } = string.Empty;
    [JsonPropertyName("avatar")] public string? Avatar { get; init; }
    [JsonPropertyName("siber_code")] public string? SiberCode { get; init; }
}
