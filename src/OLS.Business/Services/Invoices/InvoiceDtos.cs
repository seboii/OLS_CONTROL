using System.Text.Json.Serialization;
using OLS.Business.Services.Accounts;
using OLS.Business.Services.Loads;

namespace OLS.Business.Services.Invoices;

/// <summary>
/// Fatura listesi/detayı.
/// olsold: <c>Invoice</c> modeli + <c>InvoiceController::get / single</c>
///
/// İLİŞKİ ADLANDIRMASI: kaynakta ilişkiler PascalCase (<c>InvoiceType</c>,
/// <c>LoadTransferInvoiceMaps</c>). Laravel bunları <c>toArray()</c> sırasında
/// snake_case'e çevirir → <c>invoice_type</c>, <c>load_transfer_invoice_maps</c>.
/// Yük modülünün aksine burada ilişki adları FK sütun adlarıyla ÇAKIŞMIYOR
/// (<c>invoice_type_id</c> sütunu ile <c>invoice_type</c> nesnesi ayrı ayrı döner).
/// </summary>
public sealed class InvoiceDto
{
    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("invoice_type_id")] public long? InvoiceTypeId { get; init; }
    [JsonPropertyName("invoice_status_id")] public long? InvoiceStatusId { get; init; }
    [JsonPropertyName("account_id")] public long? AccountId { get; init; }

    /// <summary>0 = gelen (inbox), 1 = giden (outbox).</summary>
    [JsonPropertyName("box_type")] public short BoxType { get; init; }

    /// <summary>0 = temel fatura, 1 = ticari fatura.</summary>
    [JsonPropertyName("commercial_type")] public int CommercialType { get; init; }

    [JsonPropertyName("invoice_id")] public string? InvoiceId { get; init; }
    [JsonPropertyName("document_id")] public string? DocumentId { get; init; }
    [JsonPropertyName("target_identity_no")] public string? TargetIdentityNo { get; init; }
    [JsonPropertyName("target_title")] public string? TargetTitle { get; init; }
    [JsonPropertyName("envelope_identifier")] public string? EnvelopeIdentifier { get; init; }
    [JsonPropertyName("envelope_status_code")] public int? EnvelopeStatusCode { get; init; }
    [JsonPropertyName("message")] public string? Message { get; init; }
    [JsonPropertyName("invoice_create_date")] public DateTime InvoiceCreateDate { get; init; }
    [JsonPropertyName("invoice_execution_date")] public DateTime InvoiceExecutionDate { get; init; }
    [JsonPropertyName("payable_amount")] public decimal? PayableAmount { get; init; }
    [JsonPropertyName("tax_amount")] public decimal? TaxAmount { get; init; }
    [JsonPropertyName("tax_exclusive_amount")] public decimal? TaxExclusiveAmount { get; init; }
    [JsonPropertyName("tax_rate")] public decimal? TaxRate { get; init; }
    [JsonPropertyName("document_currency_code")] public string? DocumentCurrencyCode { get; init; }
    [JsonPropertyName("exchange_date")] public DateOnly? ExchangeDate { get; init; }
    [JsonPropertyName("exchange_rate")] public decimal? ExchangeRate { get; init; }
    [JsonPropertyName("order_document_id")] public string? OrderDocumentId { get; init; }
    [JsonPropertyName("is_archived")] public bool IsArchived { get; init; }
    [JsonPropertyName("created_by_integration")] public bool CreatedByIntegration { get; init; }
    [JsonPropertyName("created_at")] public DateTime? CreatedAt { get; init; }
    [JsonPropertyName("updated_at")] public DateTime? UpdatedAt { get; init; }

    // İlişkiler (snake_case'lenmiş PascalCase ilişki adları)
    [JsonPropertyName("invoice_type")] public InvoiceTypeDto? InvoiceType { get; init; }
    [JsonPropertyName("invoice_status")] public InvoiceStatusDto? InvoiceStatus { get; init; }
    [JsonPropertyName("invoice_account")] public AccountRefDto? InvoiceAccount { get; init; }

    [JsonPropertyName("load_transfer_invoice_maps")]
    public IReadOnlyList<LoadTransferInvoiceMapDto> LoadTransferInvoiceMaps { get; init; } = [];
}

public sealed class InvoiceTypeDto
{
    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("code")] public string? Code { get; init; }
    [JsonPropertyName("name")] public string? Name { get; init; }
}

public sealed class InvoiceStatusDto
{
    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("enum_value")] public string? EnumValue { get; init; }
    [JsonPropertyName("code")] public string? Code { get; init; }
    [JsonPropertyName("name")] public string? Name { get; init; }
}

/// <summary>
/// Fatura ↔ yük aktarma kalemi eşlemesi. Form katmanı bunu
/// <c>item.load_transfer?.id</c> ve <c>item.load_transfer_invoice_item</c>
/// olarak okuyor (<c>InvoiceFormDrawer.vue</c>).
/// </summary>
public sealed class LoadTransferInvoiceMapDto
{
    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("load_transfer_id")] public long LoadTransferId { get; init; }
    [JsonPropertyName("invoice_item_id")] public long InvoiceItemId { get; init; }
    [JsonPropertyName("invoice_id")] public long InvoiceId { get; init; }

    [JsonPropertyName("load_transfer")] public LoadTransferRefDto? LoadTransfer { get; init; }

    [JsonPropertyName("load_transfer_invoice_item")]
    public LoadTransferInvoiceItemDto? LoadTransferInvoiceItem { get; init; }
}

public sealed class LoadTransferRefDto
{
    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("load_number")] public string? LoadNumber { get; init; }
    [JsonPropertyName("load_number_work_type")] public string? LoadNumberWorkType { get; init; }
}

public sealed class LoadTransferInvoiceItemDto
{
    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("item_id")] public int? ItemId { get; init; }
    [JsonPropertyName("buysell")] public string? Buysell { get; init; }
    [JsonPropertyName("account_id")] public int? AccountId { get; init; }
    [JsonPropertyName("quantity")] public decimal? Quantity { get; init; }
    [JsonPropertyName("net_price")] public decimal? NetPrice { get; init; }
    [JsonPropertyName("tax_price")] public decimal? TaxPrice { get; init; }
    [JsonPropertyName("tax_rate")] public decimal? TaxRate { get; init; }
    [JsonPropertyName("total_price")] public decimal? TotalPrice { get; init; }
    [JsonPropertyName("description")] public string? Description { get; init; }
    [JsonPropertyName("insert_name")] public string? InsertName { get; init; }
    [JsonPropertyName("status")] public string? Status { get; init; }
    [JsonPropertyName("modulkalemid")] public string? Modulkalemid { get; init; }

    /// <summary>currency_code sütunu currencies'e FK; ilişki adı "currency".</summary>
    [JsonPropertyName("currency")] public CurrencyDto? Currency { get; init; }
    [JsonPropertyName("item")] public NamedRefDto? Item { get; init; }
}

/// <summary>Fatura dipnotu — <c>invoice_footers</c>.</summary>
public sealed class InvoiceFooterDto
{
    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("invoice_id")] public long InvoiceId { get; init; }
    [JsonPropertyName("value")] public string Value { get; init; } = string.Empty;
}

/// <summary>
/// Servis katmanının aldığı istek modeli. Form bağlama öznitelikleri
/// (<c>[FromForm(Name=...)]</c>) API katmanındaki modellerde — OLS.Business
/// MVC'ye referans vermiyor.
/// </summary>
public class InvoiceSaveRequest
{
    public short? BoxType { get; set; }
    public int? CommercialType { get; set; }
    public long? AccountId { get; set; }
    public string? Message { get; set; }
    public DateTime? InvoiceCreateDate { get; set; }
    public DateTime? InvoiceExecutionDate { get; set; }
    public string? OrderDocumentId { get; set; }
    public long? InvoiceTypeId { get; set; }
}

public sealed class InvoiceUpdateRequest : InvoiceSaveRequest
{
    public long? Id { get; set; }
    public List<InvoiceMapRequest>? LoadTransferInvoiceMaps { get; set; }
}

public sealed class InvoiceMapRequest
{
    public long LoadTransferId { get; set; }
    public long InvoiceItemId { get; set; }
}
