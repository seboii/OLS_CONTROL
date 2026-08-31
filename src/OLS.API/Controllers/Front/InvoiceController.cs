using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OLS.API.Filters;
using OLS.Business.Common;
using OLS.Business.Services.Authorization;
using OLS.Business.Services.Invoices;

namespace OLS.API.Controllers.Front;

/// <summary>
/// olsold: <c>Front\Invoice\InvoiceController</c>
///
///   GET    /api/v1/invoice                (?box_type=0|1&amp;invoice_status_id=&amp;search=)
///   GET    /api/v1/invoice/{id}
///   POST   /api/v1/invoice                (manuel gelen fatura oluştur)
///   POST   /api/v1/invoice/update
///   DELETE /api/v1/invoice/delete         (toplu: deletion_id[])
///
/// Sayfa eşlemesi: Gider Faturalar `box_type=0`, Gelir Faturalar `box_type=1`,
/// Onay Bekleyen `invoice_status_id=7&amp;box_type=0`.
///
/// UYUMSOFT'A BAĞLI OLDUĞU İÇİN PORTLANMAYANLAR (görev #8):
/// <c>GET /invoice/pdf-view/inbox|outbox</c>,
/// <c>POST /invoice/draft/send|cancel|approve</c>,
/// <c>POST /invoice/accept-or-reject</c>.
/// Bunlar bilerek eklenmedi — SOAP çağrısı olmadan 200 dönmeleri, fatura
/// gönderilmediği hâlde gönderilmiş izlenimi verirdi.
/// </summary>
[Authorize]
[Route("api/v1/invoice")]
public sealed class InvoiceController : ApiControllerBase
{
    private const string PermissionSlug = "invoice_management";

    private readonly IInvoiceService _invoices;

    public InvoiceController(IInvoiceService invoices)
    {
        _invoices = invoices;
    }

    [HttpGet]
    [RequiresPermission(PermissionAction.Read, PermissionSlug)]
    public async Task<IActionResult> Index(
        [FromQuery] string? search,
        [FromQuery(Name = "box_type")] short? boxType,
        [FromQuery(Name = "invoice_status_id")] long? invoiceStatusId,
        [FromQuery(Name = "created_by_integration")] bool? createdByIntegration,
        [FromQuery(Name = "per_page")] int? perPage,
        [FromQuery] int page = 1,
        [FromQuery(Name = "account_id")] long? accountId = null,
        [FromQuery(Name = "invoice_type_id")] long? invoiceTypeId = null,
        [FromQuery(Name = "commercial_type")] int? commercialType = null,
        [FromQuery(Name = "date_from")] DateOnly? dateFrom = null,
        [FromQuery(Name = "date_to")] DateOnly? dateTo = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _invoices.ListAsync(
            new InvoiceListQuery(search, boxType, invoiceStatusId, createdByIntegration,
                perPage, page, CurrentPath,
                accountId, invoiceTypeId, commercialType, dateFrom, dateTo),
            cancellationToken);

        return Ok(result, "Kayıtlar");
    }

    [HttpGet("{id:long}")]
    [RequiresPermission(PermissionAction.Read, PermissionSlug)]
    public async Task<IActionResult> Single(long id, CancellationToken cancellationToken)
    {
        var invoice = await _invoices.SingleAsync(id, cancellationToken);

        return invoice is null ? NotFoundError() : Ok(invoice, "Kayıtlar");
    }

    [HttpPost]
    [RequiresPermission(PermissionAction.Create, PermissionSlug)]
    public async Task<IActionResult> Create(
        [FromForm] InvoiceForm form, CancellationToken cancellationToken)
    {
        var request = form.ToRequest();

        if (Validate(request) is { Count: > 0 } errors)
            return UnprocessableEntity(ApiResponse.ValidationErrors(errors));

        var created = await _invoices.CreateAsync(request, cancellationToken);

        // Cari bulunamadıysa olsold 404 + {'error': 'Cari bulunamadı.'} dönüyor.
        if (created is null)
            return NotFound(ApiResponse.Error(Translator.Get("Cari Bulunamadı!")));

        return StatusCode(201, ApiResponse.Success(created, "Fatura başarıyla oluşturuldu."));
    }

    [HttpPost("update")]
    [RequiresPermission(PermissionAction.Update, PermissionSlug)]
    public async Task<IActionResult> Update(
        [FromForm] InvoiceUpdateForm form, CancellationToken cancellationToken)
    {
        var request = form.ToUpdateRequest();
        var errors = Validate(request);

        if (request.Id is null or <= 0)
            errors["id"] = [Translator.Get("Zorunlu Alan")];

        if (errors.Count > 0)
            return UnprocessableEntity(ApiResponse.ValidationErrors(errors));

        var updated = await _invoices.UpdateAsync(request, cancellationToken);

        return updated
            ? base.Ok(ApiResponse.Message("Fatura başarıyla güncellendi."))
            : NotFound(ApiResponse.Error(Translator.Get("Fatura Bulunamadı!")));
    }

    [HttpDelete("delete")]
    [RequiresPermission(PermissionAction.Delete, PermissionSlug)]
    public async Task<IActionResult> Delete(
        [FromBody] DeleteRequest request, CancellationToken cancellationToken)
    {
        if (request.DeletionId is not { Count: > 0 })
            return UnprocessableEntity(ApiResponse.ValidationErrors(
                new Dictionary<string, string[]> { ["deletion_id"] = [Translator.Get("Zorunlu Alan")] }));

        var deleted = await _invoices.DeleteAsync(request.DeletionId, cancellationToken);

        return deleted
            ? base.Ok(ApiResponse.Message("Fatura başarıyla silindi."))
            : NotFound(ApiResponse.Error(Translator.Get("Fatura Bulunamadı!")));
    }

    public sealed class DeleteRequest
    {
        [System.Text.Json.Serialization.JsonPropertyName("deletion_id")]
        public List<long>? DeletionId { get; set; }
    }

    /// <summary>
    /// Form gövdesi. Frontend <c>FormData</c> gönderdiği için alanlar
    /// <c>[FromForm(Name=...)]</c> ile eşlenir — form bağlayıcısı
    /// <c>[JsonPropertyName]</c>'i DİKKATE ALMAZ, model doğrudan verilirse
    /// bütün snake_case alanlar sessizce boş kalır ve 422 üretir.
    /// </summary>
    public class InvoiceForm
    {
        [FromForm(Name = "box_type")] public short? BoxType { get; set; }
        [FromForm(Name = "commercial_type")] public int? CommercialType { get; set; }
        [FromForm(Name = "account_id")] public long? AccountId { get; set; }
        [FromForm(Name = "message")] public string? Message { get; set; }
        [FromForm(Name = "invoice_create_date")] public DateTime? InvoiceCreateDate { get; set; }
        [FromForm(Name = "invoice_execution_date")] public DateTime? InvoiceExecutionDate { get; set; }
        [FromForm(Name = "order_document_id")] public string? OrderDocumentId { get; set; }
        [FromForm(Name = "invoice_type_id")] public long? InvoiceTypeId { get; set; }

        public InvoiceSaveRequest ToRequest() => Fill(new InvoiceSaveRequest());

        protected T Fill<T>(T target) where T : InvoiceSaveRequest
        {
            target.BoxType = BoxType;
            target.CommercialType = CommercialType;
            target.AccountId = AccountId;
            target.Message = Message;
            target.InvoiceCreateDate = InvoiceCreateDate;
            target.InvoiceExecutionDate = InvoiceExecutionDate;
            target.OrderDocumentId = OrderDocumentId;
            target.InvoiceTypeId = InvoiceTypeId;

            return target;
        }
    }

    public sealed class InvoiceUpdateForm : InvoiceForm
    {
        [FromForm(Name = "id")] public long? Id { get; set; }

        /// <summary>
        /// Frontend bunu <c>load_transfer_invoice_maps[0][load_transfer_id]</c>
        /// biçiminde gönderiyor; MVC bu sözdizimini listeye kendisi bağlar.
        /// </summary>
        [FromForm(Name = "load_transfer_invoice_maps")]
        public List<InvoiceMapForm>? LoadTransferInvoiceMaps { get; set; }

        public InvoiceUpdateRequest ToUpdateRequest()
        {
            var request = Fill(new InvoiceUpdateRequest());
            request.Id = Id;
            request.LoadTransferInvoiceMaps = LoadTransferInvoiceMaps?
                .Select(m => new InvoiceMapRequest
                {
                    LoadTransferId = m.LoadTransferId,
                    InvoiceItemId = m.InvoiceItemId,
                })
                .ToList();

            return request;
        }
    }

    public sealed class InvoiceMapForm
    {
        [FromForm(Name = "load_transfer_id")] public long LoadTransferId { get; set; }
        [FromForm(Name = "invoice_item_id")] public long InvoiceItemId { get; set; }
    }

    /// <summary>olsold'un <c>$request->validate([...])</c> kural setinin karşılığı.</summary>
    private Dictionary<string, string[]> Validate(InvoiceSaveRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        var required = Translator.Get("Zorunlu Alan");

        if (request.BoxType is not (0 or 1))
            errors["box_type"] = [required];

        if (request.CommercialType is not (0 or 1))
            errors["commercial_type"] = [required];

        if (request.AccountId is null or <= 0)
            errors["account_id"] = [required];

        if (request.InvoiceTypeId is null or <= 0)
            errors["invoice_type_id"] = [required];

        if (request.InvoiceCreateDate is null)
            errors["invoice_create_date"] = [required];

        if (request.InvoiceExecutionDate is null)
            errors["invoice_execution_date"] = [required];

        return errors;
    }
}

/// <summary>
/// olsold: <c>Front\Invoice\InvoiceFooterController</c>
///
///   GET    /api/v1/invoice/footer         (?invoice_id= zorunlu)
///   GET    /api/v1/invoice/footer/{id}
///   POST   /api/v1/invoice/footer
///   POST   /api/v1/invoice/footer/update
///   DELETE /api/v1/invoice/footer
/// </summary>
[Authorize]
[Route("api/v1/invoice/footer")]
public sealed class InvoiceFooterController : ApiControllerBase
{
    private const string PermissionSlug = "invoice_management";

    private readonly IInvoiceService _invoices;

    public InvoiceFooterController(IInvoiceService invoices)
    {
        _invoices = invoices;
    }

    [HttpGet]
    [RequiresPermission(PermissionAction.Read, PermissionSlug)]
    public async Task<IActionResult> Index(
        [FromQuery(Name = "invoice_id")] long? invoiceId,
        [FromQuery] string? search,
        [FromQuery(Name = "sort_column")] string sortColumn = "id",
        [FromQuery(Name = "sort_order")] string sortOrder = "asc",
        CancellationToken cancellationToken = default)
    {
        // olsold invoice_id yoksa where(null) ile boş liste döndürüyor.
        var data = invoiceId is { } id
            ? await _invoices.FootersAsync(id, search, sortColumn, sortOrder, cancellationToken)
            : [];

        return Ok(data, "Kayıt Başarılı");
    }

    [HttpGet("{id:long}")]
    [RequiresPermission(PermissionAction.Read, PermissionSlug)]
    public async Task<IActionResult> Single(long id, CancellationToken cancellationToken)
    {
        var footer = await _invoices.FooterSingleAsync(id, cancellationToken);

        return footer is null ? NotFoundError() : Ok(footer, "Kayıt Başarılı");
    }

    [HttpPost]
    [RequiresPermission(PermissionAction.Create, PermissionSlug)]
    public async Task<IActionResult> Store(
        [FromForm] FooterForm request, CancellationToken cancellationToken)
    {
        if (Validate(request.InvoiceId, request.Value) is { Count: > 0 } errors)
            return UnprocessableEntity(ApiResponse.ValidationErrors(errors));

        var created = await _invoices.FooterCreateAsync(
            request.InvoiceId!.Value, request.Value!, cancellationToken);

        return Ok(created, "Kayıt Başarılı");
    }

    [HttpPost("update")]
    [RequiresPermission(PermissionAction.Update, PermissionSlug)]
    public async Task<IActionResult> Update(
        [FromForm] FooterUpdateForm request, CancellationToken cancellationToken)
    {
        var errors = Validate(request.InvoiceId, request.Value);

        if (request.Id is null or <= 0)
            errors["id"] = [Translator.Get("Zorunlu Alan")];

        if (errors.Count > 0)
            return UnprocessableEntity(ApiResponse.ValidationErrors(errors));

        var updated = await _invoices.FooterUpdateAsync(
            request.Id!.Value, request.InvoiceId!.Value, request.Value!, cancellationToken);

        return updated is null ? NotFoundError() : Ok(updated, "Kayıt Başarılı");
    }

    [HttpDelete]
    [RequiresPermission(PermissionAction.Delete, PermissionSlug)]
    public async Task<IActionResult> Destroy(
        [FromBody] InvoiceController.DeleteRequest request, CancellationToken cancellationToken)
    {
        if (request.DeletionId is not { Count: > 0 })
            return UnprocessableEntity(ApiResponse.ValidationErrors(
                new Dictionary<string, string[]> { ["deletion_id"] = [Translator.Get("Zorunlu Alan")] }));

        var deleted = await _invoices.FooterDeleteAsync(request.DeletionId, cancellationToken);

        return deleted ? base.Ok(ApiResponse.Message("Kayıt Silindi")) : NotFoundError();
    }

    /// <summary>Form gövdesi — bkz. InvoiceForm'daki bağlayıcı notu.</summary>
    public class FooterForm
    {
        [FromForm(Name = "invoice_id")] public long? InvoiceId { get; set; }
        [FromForm(Name = "value")] public string? Value { get; set; }
    }

    public sealed class FooterUpdateForm : FooterForm
    {
        [FromForm(Name = "id")] public long? Id { get; set; }
    }

    private Dictionary<string, string[]> Validate(long? invoiceId, string? value)
    {
        var errors = new Dictionary<string, string[]>();
        var required = Translator.Get("Zorunlu Alan");

        if (invoiceId is null or <= 0)
            errors["invoice_id"] = [required];

        if (string.IsNullOrWhiteSpace(value))
            errors["value"] = [required];

        return errors;
    }
}
