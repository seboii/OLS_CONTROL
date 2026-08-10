using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OLS.API.Filters;
using OLS.Business.Common;
using OLS.Business.Services.Authorization;
using OLS.Business.Services.Expeditions;

namespace OLS.API.Controllers.Front;

/// <summary>
/// olsold: <c>Front\LoadTransfer\LoadTransferMovementController</c> — yük hareketleri.
///
///   GET    /api/v1/load_transfer_movement        (?load_id=&amp;load_transfer_id=&amp;destination_id=&amp;expedition_status_id=)
///   GET    /api/v1/load_transfer_movement/{id}
///   POST   /api/v1/load_transfer_movement
///   POST   /api/v1/load_transfer_movement/update  (PUT DEĞİL — kaynaktaki gibi)
///   DELETE /api/v1/load_transfer_movement
///
/// Yanıt zarfı bu modülde farklı: <c>{status, message, data, deleted_movements}</c>.
/// Soft delete kullanıldığı için silinmiş hareketler ayrı anahtarla döner.
/// </summary>
[Authorize]
[Route("api/v1/load_transfer_movement")]
public sealed class LoadTransferMovementController : ApiControllerBase
{
    private readonly IMovementService _movements;
    private readonly ICurrentUser _currentUser;

    public LoadTransferMovementController(IMovementService movements, ICurrentUser currentUser)
    {
        _movements = movements;
        _currentUser = currentUser;
    }

    [HttpGet]
    [RequiresPermission(PermissionAction.Read, "load_management")]
    public async Task<IActionResult> All(
        [FromQuery(Name = "load_id")] long? loadId,
        [FromQuery(Name = "load_transfer_id")] long? loadTransferId,
        [FromQuery(Name = "destination_id")] long? destinationId,
        [FromQuery(Name = "expedition_status_id")] long? expeditionStatusId,
        CancellationToken cancellationToken = default)
    {
        var result = await _movements.LoadMovementsAsync(
            new LoadMovementQuery(loadId, loadTransferId, destinationId, expeditionStatusId),
            cancellationToken);

        return base.Ok(new Dictionary<string, object?>
        {
            ["status"] = true,
            ["message"] = "Yük hareketleri başarıyla listelendi",
            ["data"] = result.Data,
            ["deleted_movements"] = result.DeletedMovements,
        });
    }

    [HttpGet("{id:long}")]
    [RequiresPermission(PermissionAction.Read, "load_management")]
    public async Task<IActionResult> Single(long id, CancellationToken cancellationToken)
    {
        var movement = await _movements.SingleLoadMovementAsync(id, cancellationToken);

        if (movement is null)
            return NotFound(new Dictionary<string, object?>
            {
                ["status"] = false,
                ["message"] = "Yük hareketi bulunamadı",
            });

        return base.Ok(new Dictionary<string, object?>
        {
            ["status"] = true,
            ["message"] = "Yük hareketi başarıyla getirildi",
            ["data"] = movement,
        });
    }

    [HttpPost]
    [RequiresPermission(PermissionAction.Create, "load_management")]
    public async Task<IActionResult> Save(
        [FromForm] MovementSaveRequest request, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.LoadNumber))
            errors["load_number"] = [Translator.Get("Zorunlu Alan")];

        if (request.DestinationId is null)
            errors["destination_id"] = [Translator.Get("Zorunlu Alan")];

        if (request.ExpeditionStatusId is null)
            errors["expedition_status_id"] = [Translator.Get("Zorunlu Alan")];

        if (errors.Count > 0)
            return UnprocessableEntity(new Dictionary<string, object?>
            {
                ["status"] = false,
                ["message"] = "Validasyon hatası",
                ["errors"] = errors,
            });

        var result = await _movements.SaveLoadMovementAsync(
            request.ToInput(), _currentUser.Id ?? 0, cancellationToken);

        if (!result.IsSuccess)
            return NotFound(new Dictionary<string, object?>
            {
                ["status"] = false,
                ["message"] = result.NotFoundMessage,
            });

        return StatusCode(StatusCodes.Status201Created, new Dictionary<string, object?>
        {
            ["status"] = true,
            ["message"] = "Yük hareketi başarıyla oluşturuldu",
            ["data"] = result.Data,
        });
    }

    /// <summary>
    /// Güncelleme KISMİ: kaynak <c>$request->has()</c> ile yalnızca gönderilen
    /// alanları yazıyor. Form gövdesinde "gönderilmedi" ile "boş gönderildi"
    /// ayrımı ancak anahtarın varlığına bakılarak yapılabilir, o yüzden
    /// <c>Request.Form</c> doğrudan okunur.
    /// </summary>
    [HttpPost("update")]
    [RequiresPermission(PermissionAction.Update, "load_management")]
    public async Task<IActionResult> Update(
        [FromForm(Name = "id")] long? id, CancellationToken cancellationToken)
    {
        if (id is null or <= 0)
            return UnprocessableEntity(new Dictionary<string, object?>
            {
                ["status"] = false,
                ["message"] = "Validasyon hatası",
                ["errors"] = new Dictionary<string, string[]> { ["id"] = [Translator.Get("Zorunlu Alan")] },
            });

        var form = Request.Form;

        string? Value(string key) => form.TryGetValue(key, out var v) ? v.ToString() : null;
        long? Number(string key) => long.TryParse(Value(key), out var v) ? v : null;

        var input = new LoadMovementInput
        {
            HasLoadNumber = form.ContainsKey("load_number"),
            LoadNumber = Value("load_number"),

            HasLoadTransferId = form.ContainsKey("load_transfer_id"),
            LoadTransferId = Number("load_transfer_id"),

            HasDestinationId = form.ContainsKey("destination_id"),
            DestinationId = Number("destination_id"),

            HasExpeditionStatusId = form.ContainsKey("expedition_status_id"),
            ExpeditionStatusId = Number("expedition_status_id"),

            HasDescription = form.ContainsKey("description"),
            Description = Value("description"),

            HasAddress = form.ContainsKey("address"),
            Address = Value("address"),
        };

        var result = await _movements.UpdateLoadMovementAsync(id.Value, input, cancellationToken);

        if (!result.IsSuccess)
            return NotFound(new Dictionary<string, object?>
            {
                ["status"] = false,
                ["message"] = result.NotFoundMessage,
            });

        return base.Ok(new Dictionary<string, object?>
        {
            ["status"] = true,
            ["message"] = "Yük hareketi başarıyla güncellendi",
            ["data"] = result.Data,
        });
    }

    /// <summary>
    /// Kaynak hem <c>deletion_id</c> dizisini hem tek <c>id</c>'yi kabul ediyor.
    /// </summary>
    [HttpDelete]
    [RequiresPermission(PermissionAction.Delete, "load_management")]
    public async Task<IActionResult> Delete(
        [FromBody] MovementDeleteRequest request, CancellationToken cancellationToken)
    {
        var ids = request.DeletionId ?? (request.Id is { } single ? [single] : []);

        await _movements.DeleteLoadMovementsAsync(ids, cancellationToken);

        // Kaynak kayıt bulunmasa da başarı döndürüyor.
        return base.Ok(new Dictionary<string, object?>
        {
            ["status"] = true,
            ["message"] = "Yük hareketi başarıyla silindi",
        });
    }

    /// <summary>Arayüz <c>FormData</c> gönderiyor — form bağlayıcısı adları burada verilir.</summary>
    public sealed class MovementSaveRequest
    {
        [FromForm(Name = "load_number")] public string? LoadNumber { get; set; }
        [FromForm(Name = "load_transfer_id")] public long? LoadTransferId { get; set; }
        [FromForm(Name = "destination_id")] public long? DestinationId { get; set; }
        [FromForm(Name = "expedition_status_id")] public long? ExpeditionStatusId { get; set; }
        [FromForm(Name = "description")] public string? Description { get; set; }
        [FromForm(Name = "address")] public string? Address { get; set; }

        public LoadMovementInput ToInput() => new()
        {
            LoadNumber = LoadNumber,
            LoadTransferId = LoadTransferId,
            DestinationId = DestinationId,
            ExpeditionStatusId = ExpeditionStatusId,
            Description = Description,
            Address = Address,
        };
    }

    public sealed class MovementDeleteRequest
    {
        [JsonPropertyName("deletion_id")] public List<long>? DeletionId { get; set; }
        [JsonPropertyName("id")] public long? Id { get; set; }
    }
}
