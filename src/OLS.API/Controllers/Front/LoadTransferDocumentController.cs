using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OLS.API.Filters;
using OLS.Business.Common;
using OLS.Business.Services.Authorization;
using OLS.Business.Services.LoadTransfers;

namespace OLS.API.Controllers.Front;

/// <summary>
/// Evrak Takibi — Yük detayının yeni sekmesi. olsold'da karşılığı yok (yeni
/// özellik); Hareketler (LoadTransferMovementController) gibi bağımsız, anlık
/// kaydedilen bir alt-kaynak, ama Siber'e de yazıyor (skn_yukevrak).
///
///   GET    /api/v1/load_transfer_document        (?load_transfer_id=)
///   POST   /api/v1/load_transfer_document
///   POST   /api/v1/load_transfer_document/update
///   DELETE /api/v1/load_transfer_document
/// </summary>
[Authorize]
[Route("api/v1/load_transfer_document")]
public sealed class LoadTransferDocumentController : ApiControllerBase
{
    private readonly ILoadTransferDocumentService _documents;

    public LoadTransferDocumentController(ILoadTransferDocumentService documents) => _documents = documents;

    [HttpGet]
    [RequiresPermission(PermissionAction.Read, "load_management")]
    public async Task<IActionResult> All(
        [FromQuery(Name = "load_transfer_id")] long? loadTransferId, CancellationToken cancellationToken)
    {
        if (loadTransferId is null)
            return base.Ok(new Dictionary<string, object?>
            {
                ["status"] = true,
                ["message"] = "Evrak listesi başarıyla listelendi",
                ["data"] = Array.Empty<object>(),
            });

        var result = await _documents.ListAsync(loadTransferId.Value, cancellationToken);

        return base.Ok(new Dictionary<string, object?>
        {
            ["status"] = true,
            ["message"] = "Evrak listesi başarıyla listelendi",
            ["data"] = result,
        });
    }

    [HttpPost]
    [RequiresPermission(PermissionAction.Create, "load_management")]
    public async Task<IActionResult> Save(
        [FromBody] LoadTransferDocumentRequest request, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.LoadTransferId is null or <= 0)
            errors["load_transfer_id"] = [Translator.Get("Zorunlu Alan")];

        if (request.EvrakTuruId is null or <= 0)
            errors["evrak_turu_id"] = [Translator.Get("Zorunlu Alan")];

        if (errors.Count > 0)
            return UnprocessableEntity(new Dictionary<string, object?>
            {
                ["status"] = false,
                ["message"] = "Validasyon hatası",
                ["errors"] = errors,
            });

        var result = await _documents.SaveAsync(request.ToInput(), cancellationToken);

        if (!result.IsSuccess)
            return UnprocessableEntity(new Dictionary<string, object?>
            {
                ["status"] = false,
                ["message"] = result.ErrorMessage,
            });

        return StatusCode(StatusCodes.Status201Created, new Dictionary<string, object?>
        {
            ["status"] = true,
            ["message"] = "Evrak başarıyla eklendi",
            ["data"] = result.Data,
        });
    }

    [HttpPost("update")]
    [RequiresPermission(PermissionAction.Update, "load_management")]
    public async Task<IActionResult> Update(
        [FromBody] LoadTransferDocumentUpdateRequest request, CancellationToken cancellationToken)
    {
        if (request.Id is null or <= 0)
            return UnprocessableEntity(new Dictionary<string, object?>
            {
                ["status"] = false,
                ["message"] = "Validasyon hatası",
                ["errors"] = new Dictionary<string, string[]> { ["id"] = [Translator.Get("Zorunlu Alan")] },
            });

        var result = await _documents.UpdateAsync(request.Id.Value, request.ToInput(), cancellationToken);

        if (!result.IsSuccess)
            return UnprocessableEntity(new Dictionary<string, object?>
            {
                ["status"] = false,
                ["message"] = result.ErrorMessage,
            });

        return base.Ok(new Dictionary<string, object?>
        {
            ["status"] = true,
            ["message"] = "Evrak başarıyla güncellendi",
            ["data"] = result.Data,
        });
    }

    [HttpDelete]
    [RequiresPermission(PermissionAction.Delete, "load_management")]
    public async Task<IActionResult> Delete(
        [FromBody] LoadTransferDocumentDeleteRequest request, CancellationToken cancellationToken)
    {
        var deleted = request.Id is { } id && await _documents.DeleteAsync(id, cancellationToken);

        if (!deleted)
            return NotFound(new Dictionary<string, object?>
            {
                ["status"] = false,
                ["message"] = "Evrak kaydı bulunamadı",
            });

        return base.Ok(new Dictionary<string, object?>
        {
            ["status"] = true,
            ["message"] = "Evrak başarıyla silindi",
        });
    }

    public class LoadTransferDocumentRequest
    {
        [JsonPropertyName("load_transfer_id")] public long? LoadTransferId { get; set; }
        [JsonPropertyName("evrak_turu_id")] public long? EvrakTuruId { get; set; }
        [JsonPropertyName("document_number")] public string? DocumentNumber { get; set; }
        [JsonPropertyName("date")] public DateOnly? Date { get; set; }
        [JsonPropertyName("original_count")] public int? OriginalCount { get; set; }
        [JsonPropertyName("copy_count")] public int? CopyCount { get; set; }
        [JsonPropertyName("delivered_to")] public string? DeliveredTo { get; set; }
        [JsonPropertyName("delivered_at")] public DateOnly? DeliveredAt { get; set; }
        [JsonPropertyName("note")] public string? Note { get; set; }

        public LoadTransferDocumentInput ToInput() => new()
        {
            LoadTransferId = LoadTransferId ?? 0,
            EvrakTuruId = EvrakTuruId ?? 0,
            DocumentNumber = DocumentNumber,
            Date = Date,
            OriginalCount = OriginalCount,
            CopyCount = CopyCount,
            DeliveredTo = DeliveredTo,
            DeliveredAt = DeliveredAt,
            Note = Note,
        };
    }

    public sealed class LoadTransferDocumentUpdateRequest : LoadTransferDocumentRequest
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
    }

    public sealed class LoadTransferDocumentDeleteRequest
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
    }
}
