using Microsoft.AspNetCore.Mvc;
using OLS.Business.Services.Accounts;

namespace OLS.API.Controllers.Front;

/// <summary>
/// Cari formu (multipart/form-data).
///
/// Frontend <c>AccountFormDrawer.vue</c> alanları FormData ile gönderiyor;
/// dizi alanlar PHP tarzı köşeli parantezle: <c>account_type_mapping[]</c>,
/// <c>contact_persons[0][name]</c>. Bu yüzden bağlama adları elle veriliyor.
/// </summary>
public sealed class AccountFormRequest
{
    [FromForm(Name = "id")] public long? Id { get; set; }
    /// <summary>Kaydın açılacağı şirket; yalnızca süper adminde dikkate alınır.</summary>
    [FromForm(Name = "siber_company_id")] public string? SiberCompanyId { get; set; }
    [FromForm(Name = "name")] public string? Name { get; set; }
    [FromForm(Name = "tax_number")] public string? TaxNumber { get; set; }
    [FromForm(Name = "tax_office")] public string? TaxOffice { get; set; }
    [FromForm(Name = "invoice_type")] public int? InvoiceType { get; set; }
    [FromForm(Name = "country_id")] public Guid? CountryId { get; set; }
    [FromForm(Name = "city_id")] public Guid? CityId { get; set; }
    [FromForm(Name = "district_id")] public Guid? DistrictId { get; set; }
    [FromForm(Name = "address")] public string? Address { get; set; }
    [FromForm(Name = "phone")] public string? Phone { get; set; }
    [FromForm(Name = "phone_country_id")] public Guid? PhoneCountryId { get; set; }
    [FromForm(Name = "email")] public string? Email { get; set; }
    [FromForm(Name = "contact_person")] public string? ContactPerson { get; set; }
    /// <summary>
    /// olsold: <c>discount: required</c> — nullable tutuluyor ki "hiç gönderilmedi"
    /// (reddedilmeli) ile "0 gönderildi" (geçerli) ayırt edilebilsin.
    /// </summary>
    [FromForm(Name = "discount")] public int? Discount { get; set; }
    [FromForm(Name = "individual_personal")] public string? IndividualPersonal { get; set; }
    [FromForm(Name = "contact_language")] public string? ContactLanguage { get; set; }

    [FromForm(Name = "avatar")] public IFormFile? Avatar { get; set; }

    /// <summary>1 ise mevcut avatar silinir (olsold: avatar_remove).</summary>
    [FromForm(Name = "avatar_remove")] public int AvatarRemove { get; set; }

    [FromForm(Name = "account_type_mapping")] public List<int> AccountTypeMapping { get; set; } = [];
    [FromForm(Name = "account_charge_person")] public List<int> AccountChargePerson { get; set; } = [];

    [FromForm(Name = "contact_persons")] public List<ContactPersonForm> ContactPersons { get; set; } = [];

    public AccountWriteModel ToWriteModel(string? avatarFileName) => new()
    {
        SiberCompanyId = SiberCompanyId,
        Id = Id,
        Name = Name,
        TaxNumber = TaxNumber,
        TaxOffice = TaxOffice,
        InvoiceType = InvoiceType,
        CountryId = CountryId,
        CityId = CityId,
        DistrictId = DistrictId,
        Address = Address,
        Phone = Phone,
        PhoneCountryId = PhoneCountryId,
        Email = Email,
        ContactPerson = ContactPerson,
        Discount = Discount ?? 0,
        IndividualPersonal = IndividualPersonal,
        ContactLanguage = ContactLanguage,
        AvatarFileName = avatarFileName,
        AvatarRemoved = AvatarRemove == 1,
        AccountTypeMapping = AccountTypeMapping,
        AccountChargePersons = AccountChargePerson,
        ContactPersons = ContactPersons
            .Select(p => new ContactPersonInput(p.Name, p.Email))
            .ToList(),
    };
}

public sealed class ContactPersonForm
{
    [FromForm(Name = "name")] public string? Name { get; set; }
    [FromForm(Name = "email")] public string? Email { get; set; }
}

/// <summary>
/// olsold'un toplu silme gövdesi: <c>{ "deletion_id": [1,2,3] }</c>.
/// Tüm modüllerde aynı şekil kullanılıyor.
/// </summary>
public sealed class DeletionRequest
{
    [System.Text.Json.Serialization.JsonPropertyName("deletion_id")]
    public List<long> DeletionId { get; set; } = [];
}
