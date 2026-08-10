using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OLS.API.Filters;
using OLS.Business.Common;
using OLS.Business.Services.Authorization;
using OLS.Business.Services.LoadTransfers;

namespace OLS.API.Controllers.Front;

/// <summary>
/// olsold: <c>Front\LoadTransferInvoiceItem\LoadTransferInvoiceItemController</c>
///
///   GET    /api/v1/load_transfer_invoice_item        (?search=&amp;status=&amp;buysell=&amp;account_id=&amp;per_page=)
///   GET    /api/v1/load_transfer_invoice_item/{id}
///   POST   /api/v1/load_transfer_invoice_item
///   PUT    /api/v1/load_transfer_invoice_item
///   DELETE /api/v1/load_transfer_invoice_item
///
/// Arayüz: fatura formundaki "Kalemler" penceresi (<c>InvoiceFormInvoiceItems.vue</c>).
///
/// Not: <c>DELETE /load_transfer/load_transfer_invoice_item</c> ayrı bir uçtur
/// (yük aktarma formundan kalem silme) ve <c>LoadTransferController</c>'da durur.
/// Arayüzün <c>deleteUrl</c>'i <c>…/load_transfer_invoice_item/delete</c> yazıyor
/// ama kaynakta böyle bir rota yok — bu düğme olsold'da da 404 veriyordu.
/// </summary>
[Authorize]
[Route("api/v1/load_transfer_invoice_item")]
public sealed class LoadTransferInvoiceItemController : ApiControllerBase
{
    private readonly ILoadTransferInvoiceItemService _items;

    public LoadTransferInvoiceItemController(ILoadTransferInvoiceItemService items)
    {
        _items = items;
    }

    [HttpGet]
    public async Task<IActionResult> All(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] string? buysell,
        [FromQuery(Name = "account_id")] long? accountId,
        [FromQuery(Name = "per_page")] int? perPage,
        [FromQuery] int page = 1,
        CancellationToken cancellationToken = default)
    {
        var result = await _items.ListAsync(
            new InvoiceItemQuery(search, status, buysell, accountId, perPage, page, CurrentPath),
            cancellationToken);

        return Ok(result, "Kayıtlar");
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Single(long id, CancellationToken cancellationToken)
    {
        var item = await _items.SingleAsync(id, cancellationToken);

        return item is null ? NotFoundError() : Ok(item, "Kayıtlar");
    }

    [HttpPost]
    public async Task<IActionResult> Save(
        [FromBody] InvoiceItemRequest request, CancellationToken cancellationToken)
    {
        var created = await _items.CreateAsync(request.Name, request.SiberId, cancellationToken);

        return Ok(created, "Kayıt Başarılı");
    }

    [HttpPut]
    public async Task<IActionResult> Update(
        [FromBody] InvoiceItemRequest request, CancellationToken cancellationToken)
    {
        if (request.Id is null or <= 0)
            return UnprocessableEntity(ApiResponse.ValidationErrors(
                new Dictionary<string, string[]> { ["id"] = [Translator.Get("Zorunlu Alan")] }));

        var updated = await _items.UpdateAsync(
            request.Id.Value, request.Name, request.SiberId, cancellationToken);

        return updated is null ? NotFoundError() : Ok(updated, "Güncelleme Başarılı");
    }

    /// <summary>Kaynak burada <c>payment_management</c> yetkisini kontrol ediyor.</summary>
    [HttpDelete]
    [RequiresPermission(PermissionAction.Delete, "payment_management")]
    public async Task<IActionResult> Delete(
        [FromBody] InvoiceController.DeleteRequest request, CancellationToken cancellationToken)
    {
        if (request.DeletionId is not { Count: > 0 })
            return UnprocessableEntity(ApiResponse.ValidationErrors(
                new Dictionary<string, string[]> { ["deletion_id"] = [Translator.Get("Zorunlu Alan")] }));

        var deleted = await _items.DeleteAsync(request.DeletionId, cancellationToken);

        return deleted
            ? base.Ok(ApiResponse.Message("Kayıt Başarıyla Silindi"))
            : NotFoundError();
    }

    /// <summary>
    /// Kaynak <c>name</c> / <c>siber_id</c> adlarını kullanıyor; tabloda
    /// karşılıkları <c>insert_name</c> / <c>modulkalemid</c>
    /// (bkz. <see cref="ILoadTransferInvoiceItemService.CreateAsync"/>).
    /// </summary>
    public sealed class InvoiceItemRequest
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("siber_id")] public string? SiberId { get; set; }
    }
}
