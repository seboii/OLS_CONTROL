using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OLS.API.Filters;
using OLS.Business.Common;
using OLS.Business.Services.Authorization;
using OLS.Business.Services.Expeditions;

namespace OLS.API.Controllers.Front;

/// <summary>
/// olsold: <c>Front\ExpeditionLoadMapping\ExpeditionLoadMappingController</c>
///
///   GET    /api/v1/expedition_load_mapping        eklenebilir (eşlenmemiş) yükler
///   GET    /api/v1/expedition_load_mapping/{id}   SEFERİN eşlenmiş yükleri + toplamlar
///   POST   /api/v1/expedition_load_mapping        {expedition_id, load_transfer_id}
///   POST   /api/v1/expedition_load_mapping/update yalnızca Siber'i günceller
///   DELETE /api/v1/expedition_load_mapping        {"deletion_id":[…]}
///
/// Arayüz: <c>ExpeditionLoad.vue</c> — liste penceresinden yük seçilince POST,
/// satır silinince DELETE, sefer açıldığında <c>/{id}</c>.
///
/// DİKKAT: güncelleme <c>PUT</c> değil <c>POST /update</c> (kaynaktaki gibi).
/// </summary>
[Authorize]
[Route("api/v1/expedition_load_mapping")]
public sealed class ExpeditionLoadMappingController : ApiControllerBase
{
    private readonly IExpeditionLoadMappingService _mappings;

    public ExpeditionLoadMappingController(IExpeditionLoadMappingService mappings)
    {
        _mappings = mappings;
    }

    [HttpGet]
    [RequiresPermission(PermissionAction.Read, "expedition_management")]
    public async Task<IActionResult> All(
        [FromQuery] string? search,
        [FromQuery(Name = "per_page")] int? perPage,
        [FromQuery] int page = 1,
        CancellationToken cancellationToken = default)
    {
        var result = await _mappings.AvailableLoadsAsync(
            search, perPage, page, CurrentPath, cancellationToken);

        return Ok(result, "Kayıtlar");
    }

    /// <summary>
    /// Yanıtta <c>total_expedition_values</c> zarfın DIŞINDA, kökte yer alır —
    /// arayüz <c>res.total_expedition_values</c> okuyor.
    /// </summary>
    [HttpGet("{id:long}")]
    [RequiresPermission(PermissionAction.Read, "expedition_management")]
    public async Task<IActionResult> Single(long id, CancellationToken cancellationToken)
    {
        var result = await _mappings.ByExpeditionAsync(id, cancellationToken);

        return base.Ok(new Dictionary<string, object?>
        {
            ["data"] = result.Data,
            ["total_expedition_values"] = result.Totals,
            ["message"] = Translator.Get("Kayıtlar"),
        });
    }

    [HttpPost]
    [RequiresPermission(PermissionAction.Create, "expedition_management")]
    public async Task<IActionResult> Save(
        [FromBody] MappingSaveRequest request, CancellationToken cancellationToken)
    {
        var result = await _mappings.SaveAsync(
            request.ExpeditionId, request.LoadTransferId, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(ApiResponse.ValidationErrors(new Dictionary<string, string[]>
            {
                ["message"] = [result.ErrorMessage!],
            }));

        return base.Ok(new Dictionary<string, object?>
        {
            ["siber_id"] = result.SiberId,
            ["message"] = Translator.Get("Kayıt Başarılı"),
        });
    }

    [HttpPost("update")]
    [RequiresPermission(PermissionAction.Update, "expedition_management")]
    public async Task<IActionResult> Update(
        [FromBody] MappingUpdateRequest request, CancellationToken cancellationToken)
    {
        await _mappings.UpdateAsync(new ExpeditionMappingUpdateInput
        {
            MappingSiberId = request.MappingSiberId,
            LoadTransferId = request.LoadTransferId,
            ExpeditionId = request.ExpeditionId,
            RomorkId = request.RomorkId,
            YerId = request.YerId,
            Date = request.Date,
        }, cancellationToken);

        return base.Ok(ApiResponse.Message("Güncelleme Başarılı"));
    }

    [HttpDelete]
    [RequiresPermission(PermissionAction.Delete, "expedition_management")]
    public async Task<IActionResult> Delete(
        [FromBody] InvoiceController.DeleteRequest request, CancellationToken cancellationToken)
    {
        if (request.DeletionId is not { Count: > 0 })
            return UnprocessableEntity(ApiResponse.ValidationErrors(
                new Dictionary<string, string[]> { ["deletion_id"] = [Translator.Get("Zorunlu Alan")] }));

        var deleted = await _mappings.DeleteAsync(request.DeletionId, cancellationToken);

        return deleted
            ? base.Ok(ApiResponse.Message("Kayıt Başarıyla Silindi"))
            : NotFoundError();
    }

    public sealed class MappingSaveRequest
    {
        [JsonPropertyName("expedition_id")] public long? ExpeditionId { get; set; }
        [JsonPropertyName("load_transfer_id")] public long? LoadTransferId { get; set; }
    }

    /// <summary>
    /// Kaynak alan adını <c>expdition_load_mapping_id</c> diye yazmış (yazım
    /// hatası); arayüz bu ucu çağırmıyor ama uyum için aynı ad korundu.
    /// </summary>
    public sealed class MappingUpdateRequest
    {
        [JsonPropertyName("expdition_load_mapping_id")] public string? MappingSiberId { get; set; }
        [JsonPropertyName("load_transfer_id")] public string? LoadTransferId { get; set; }
        [JsonPropertyName("expedition_id")] public string? ExpeditionId { get; set; }
        [JsonPropertyName("romork_id")] public string? RomorkId { get; set; }
        [JsonPropertyName("yer_id")] public string? YerId { get; set; }
        [JsonPropertyName("date")] public DateTime? Date { get; set; }
    }
}
