using Microsoft.AspNetCore.Mvc;
using OLS.Business.Common;
using OLS.Business.Services.Loads;

namespace OLS.API.Controllers.Front;

/// <summary>
/// Yük/Teklif formu (multipart/form-data — dosya eki taşıdığı için).
///
/// Frontend <c>OfferFormDrawer.vue</c> / <c>LoadFormDrawer.vue</c> alanları
/// PHP tarzı dizi adlarıyla gönderiyor: <c>load_content[0][quantity]</c>,
/// <c>load_charge_person[0][user_id]</c>, <c>email[to][]</c>, <c>files[0][file]</c>.
/// </summary>
public sealed class LoadFormRequest
{
    [FromForm(Name = "work_type_id")] public int? WorkTypeId { get; set; }
    [FromForm(Name = "loading_type_id")] public int? LoadingTypeId { get; set; }
    [FromForm(Name = "payment_type_id")] public int? PaymentTypeId { get; set; }
    [FromForm(Name = "status_type_id")] public int? StatusTypeId { get; set; }
    [FromForm(Name = "offer_date")] public DateOnly? OfferDate { get; set; }
    [FromForm(Name = "offer_validity_date")] public DateOnly? OfferValidityDate { get; set; }
    [FromForm(Name = "marketing_notification_date")] public DateOnly? MarketingNotificationDate { get; set; }
    [FromForm(Name = "load_transfer_type_id")] public int? LoadTransferTypeId { get; set; }
    [FromForm(Name = "instruction_id")] public int? InstructionId { get; set; }
    [FromForm(Name = "romork_type_id")] public int? RomorkTypeId { get; set; }
    [FromForm(Name = "customer_id")] public int? CustomerId { get; set; }
    [FromForm(Name = "sender_id")] public int? SenderId { get; set; }
    [FromForm(Name = "receiver_id")] public int? ReceiverId { get; set; }
    [FromForm(Name = "company_pay_freight_id")] public int? CompanyPayFreightId { get; set; }
    [FromForm(Name = "agent_id")] public int? AgentId { get; set; }
    [FromForm(Name = "payer_company")] public string? PayerCompany { get; set; }
    [FromForm(Name = "description")] public string? Description { get; set; }
    [FromForm(Name = "departure_country_id")] public Guid? DepartureCountryId { get; set; }
    [FromForm(Name = "transit_country_id")] public Guid? TransitCountryId { get; set; }
    [FromForm(Name = "target_country_id")] public Guid? TargetCountryId { get; set; }
    [FromForm(Name = "department_id")] public int? DepartmentId { get; set; }
    [FromForm(Name = "front_transportation_by_us")] public int FrontTransportationByUs { get; set; }
    [FromForm(Name = "final_transportation_by_us")] public int FinalTransportationByUs { get; set; }
    [FromForm(Name = "way_of_working")] public int WayOfWorking { get; set; }

    [FromForm(Name = "load_content")] public List<LoadContentForm> LoadContent { get; set; } = [];
    [FromForm(Name = "load_financial_item")] public List<LoadFinancialItemForm> LoadFinancialItem { get; set; } = [];
    [FromForm(Name = "load_charge_person")] public List<LoadChargePersonForm> LoadChargePerson { get; set; } = [];
    [FromForm(Name = "load_movement")] public List<LoadMovementForm> LoadMovement { get; set; } = [];

    [FromForm(Name = "email_to")] public List<string> EmailTo { get; set; } = [];
    [FromForm(Name = "email_cc")] public List<string> EmailCc { get; set; } = [];

    [FromForm(Name = "files")] public List<IFormFile> Files { get; set; } = [];

    /// <summary>Güncellemede korunacak mevcut dosya id'leri.</summary>
    [FromForm(Name = "existing_file_ids")] public List<long> ExistingFileIds { get; set; } = [];

    public LoadWriteModel ToModel(long currentUserId, IReadOnlyList<UploadedFile> uploaded) => new()
    {
        CurrentUserId = currentUserId,
        WorkTypeId = WorkTypeId,
        LoadingTypeId = LoadingTypeId,
        PaymentTypeId = PaymentTypeId,
        StatusTypeId = StatusTypeId,
        OfferDate = OfferDate,
        OfferValidityDate = OfferValidityDate,
        MarketingNotificationDate = MarketingNotificationDate,
        LoadTransferTypeId = LoadTransferTypeId,
        InstructionId = InstructionId,
        RomorkTypeId = RomorkTypeId,
        CustomerId = CustomerId,
        SenderId = SenderId,
        ReceiverId = ReceiverId,
        CompanyPayFreightId = CompanyPayFreightId,
        AgentId = AgentId,
        PayerCompany = PayerCompany,
        Description = Description,
        DepartureCountryId = DepartureCountryId,
        TransitCountryId = TransitCountryId,
        TargetCountryId = TargetCountryId,
        DepartmentId = DepartmentId,
        FrontTransportationByUs = FrontTransportationByUs,
        FinalTransportationByUs = FinalTransportationByUs,
        WayOfWorking = WayOfWorking,

        Contents = LoadContent.Select(c => new LoadContentInput(
            c.ProductTypeId, c.CaseTypeId, TurkishDecimal.ParseInt(c.Quantity),
            TurkishDecimal.Parse(c.Width), TurkishDecimal.Parse(c.Height),
            TurkishDecimal.Parse(c.Length), TurkishDecimal.Parse(c.GrossWeight),
            TurkishDecimal.Parse(c.NetWeight), TurkishDecimal.Parse(c.Volume),
            TurkishDecimal.Parse(c.Lademeter), c.Stackable)).ToList(),

        FinancialItems = LoadFinancialItem.Select(f => new LoadFinancialItemInput(
            f.Item, TurkishDecimal.ParseInt(f.Quantity), f.TransportTypeId, f.AccountId,
            f.Description, f.Order,
            TurkishDecimal.Parse(f.NetPrice), TurkishDecimal.Parse(f.TotalPrice),
            f.Currency, f.Buysell)).ToList(),

        ChargePersons = LoadChargePerson
            .Select(p => new LoadChargePersonInput(p.UserId, p.UserType)).ToList(),

        Movements = LoadMovement
            .Select(m => new LoadMovementInput(m.MovementTypeId, m.Note)).ToList(),

        EmailTo = EmailTo,
        EmailCc = EmailCc,
        NewFiles = uploaded,
        KeepFileIds = ExistingFileIds,
    };

}

/// <summary>
/// Ondalık alanlar METİN olarak alınır: kullanıcı Türkçe klavyede virgül
/// kullanıyor ve varsayılan model bağlayıcısı "32,5" değerini 325 okuyor.
/// Ayrıştırma <see cref="TurkishDecimal"/> ile yapılır.
/// </summary>
public sealed class LoadContentForm
{
    [FromForm(Name = "product_type_id")] public int? ProductTypeId { get; set; }
    [FromForm(Name = "case_type_id")] public int? CaseTypeId { get; set; }
    [FromForm(Name = "quantity")] public string? Quantity { get; set; }
    [FromForm(Name = "width")] public string? Width { get; set; }
    [FromForm(Name = "height")] public string? Height { get; set; }
    [FromForm(Name = "length")] public string? Length { get; set; }
    [FromForm(Name = "gross_weight")] public string? GrossWeight { get; set; }
    [FromForm(Name = "net_weight")] public string? NetWeight { get; set; }
    [FromForm(Name = "volume")] public string? Volume { get; set; }
    [FromForm(Name = "lademeter")] public string? Lademeter { get; set; }
    [FromForm(Name = "stackable")] public int? Stackable { get; set; }
}

public sealed class LoadFinancialItemForm
{
    [FromForm(Name = "item")] public int? Item { get; set; }
    [FromForm(Name = "quantity")] public string? Quantity { get; set; }
    [FromForm(Name = "transport_type_id")] public int? TransportTypeId { get; set; }
    [FromForm(Name = "account_id")] public int? AccountId { get; set; }
    [FromForm(Name = "description")] public string? Description { get; set; }
    [FromForm(Name = "order")] public int? Order { get; set; }
    [FromForm(Name = "buysell")] public int? Buysell { get; set; }
    [FromForm(Name = "currency")] public int? Currency { get; set; }

    // Fiyatlar metin olarak alınır; virgüllü girişler desteklenmeli.
    [FromForm(Name = "net_price")] public string? NetPrice { get; set; }
    [FromForm(Name = "total_price")] public string? TotalPrice { get; set; }
}

public sealed class LoadChargePersonForm
{
    [FromForm(Name = "user_id")] public int? UserId { get; set; }
    [FromForm(Name = "user_type")] public int? UserType { get; set; }
}

public sealed class LoadMovementForm
{
    [FromForm(Name = "movement_type_id")] public int? MovementTypeId { get; set; }
    [FromForm(Name = "note")] public string? Note { get; set; }
}
