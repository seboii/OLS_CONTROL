using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OLS.API.Filters;
using OLS.Business.Common;
using OLS.Business.Services.Authorization;
using OLS.Business.Services.Expeditions;
using OLS.Business.Services.LoadTransfers;

namespace OLS.API.Controllers.Front;

/// <summary>
/// olsold: <c>Front\LoadTransfer\LoadTransferController</c> — Yük Aktarma.
///
/// Siber'den içe aktarılan GEÇMİŞ yük kayıtları burada listelenir.
/// Yazma tarafı (save/update/delete + Siber senkronu) henüz portlanmadı.
/// </summary>
[Authorize]
[Route("api/v1/load_transfer")]
public sealed class LoadTransferController : ApiControllerBase
{
    private readonly ILoadTransferService _transfers;
    private readonly ILoadTransferWriteService _write;
    private readonly ILoadTransferUpdateService _update;
    private readonly ICurrentUser _currentUser;

    public LoadTransferController(
        ILoadTransferService transfers,
        ILoadTransferWriteService write,
        ILoadTransferUpdateService update,
        ICurrentUser currentUser)
    {
        _transfers = transfers;
        _write = write;
        _update = update;
        _currentUser = currentUser;
    }

    [HttpGet]
    [RequiresPermission(PermissionAction.Read, "load_management")]
    public async Task<IActionResult> All(
        [FromQuery] string? search,
        [FromQuery(Name = "work_type_id")] int? workTypeId,
        [FromQuery(Name = "date_from")] DateOnly? dateFrom,
        [FromQuery(Name = "date_to")] DateOnly? dateTo,
        [FromQuery(Name = "per_page")] int? perPage,
        [FromQuery] int page = 1,
        CancellationToken cancellationToken = default)
    {
        var result = await _transfers.ListAsync(
            new LoadTransferListQuery(search, workTypeId, dateFrom, dateTo, perPage, page, CurrentPath),
            cancellationToken);

        return Ok(result, "Kayıtlar");
    }

    [HttpGet("{id:long}")]
    [RequiresPermission(PermissionAction.Read, "load_management")]
    public async Task<IActionResult> Single(long id, CancellationToken cancellationToken)
    {
        var transfer = await _transfers.SingleAsync(id, cancellationToken);

        return transfer is null ? NotFoundError() : Ok(transfer, "Kayıtlar");
    }

    public sealed class ConvertOfferRequest
    {
        /// <summary>Teklifin Siber kimliği (loads.siber_id).</summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }
    }

    /// <summary>
    /// Onaylanmış teklifi yüke dönüştürür. olsold'da bu uç <c>save</c> adıyla
    /// duruyordu ama davranışı "kayıt oluştur" değil "teklifi yüke çevir"dir.
    /// </summary>
    [HttpPost]
    [RequiresPermission(PermissionAction.Create, "load_management")]
    public async Task<IActionResult> ConvertOffer(
        [FromBody] ConvertOfferRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Id))
            return BadRequestError("Form hataydı! Lütfen geliştiricinizle iletişime geçin.");

        if (_currentUser.Id is not { } userId)
            return Unauthorized(ApiResponse.Error(Translator.Get("Yetkisiz Erişim")));

        var result = await _write.ConvertOfferAsync(request.Id, userId, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(ApiResponse.ValidationErrors(new Dictionary<string, string[]>
            {
                ["message"] = [result.ErrorMessage!],
            }));

        return base.Ok(ApiResponse.Success(
            new { yuk_no = result.LoadNumber }, "Yük başarıyla oluşturuldu"));
    }

    /// <summary>
    /// olsold: <c>POST /load_transfer/{id}</c> — dönüşüm sonrası düzenleme.
    ///
    /// <c>POST /load_transfer</c> (id'siz) teklifi yüke DÖNÜŞTÜRÜR; id verilen
    /// çağrı ise mevcut yükü GÜNCELLER. Kaynakta da aynı ayrım var
    /// (<c>Route::post('/{id?}')</c>).
    ///
    /// Yük numarası ve iş türü güncellenmez — Siber'de başka kayıtlarla
    /// ilişkilendirilmiş durumdalar.
    /// </summary>
    [HttpPost("{id:long}")]
    [RequiresPermission(PermissionAction.Update, "load_management")]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] LoadTransferUpdateRequest request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.Id is not { } userId)
            return Unauthorized(ApiResponse.Error(Translator.Get("Yetkisiz Erişim")));

        request.Id = id;

        var result = await _update.UpdateAsync(request, userId, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(ApiResponse.ValidationErrors(new Dictionary<string, string[]>
            {
                ["message"] = [result.ErrorMessage!],
            }));

        return base.Ok(ApiResponse.Success(
            new { yuk_no = result.LoadNumber }, "Güncelleme Başarılı"));
    }

    [HttpDelete]
    [RequiresPermission(PermissionAction.Delete, "load_management")]
    public async Task<IActionResult> Delete(
        [FromBody] DeletionRequest request, CancellationToken cancellationToken)
    {
        if (request.DeletionId.Count == 0)
            return BadRequestError("Form hataydı! Lütfen geliştiricinizle iletişime geçin.");

        await _write.DeleteAsync(request.DeletionId, cancellationToken);

        return OkMessage("Kayıt Başarıyla Silindi");
    }

    [HttpDelete("load_transfer_package")]
    [RequiresPermission(PermissionAction.Delete, "load_management")]
    public async Task<IActionResult> DeletePackages(
        [FromBody] DeletionRequest request, CancellationToken cancellationToken)
    {
        if (request.DeletionId.Count == 0)
            return BadRequestError("Form hataydı! Lütfen geliştiricinizle iletişime geçin.");

        await _write.DeletePackagesAsync(request.DeletionId, cancellationToken);

        return OkMessage("Kayıt Başarıyla Silindi");
    }

    [HttpDelete("load_transfer_invoice_item")]
    [RequiresPermission(PermissionAction.Delete, "load_management")]
    public async Task<IActionResult> DeleteInvoiceItems(
        [FromBody] DeletionRequest request, CancellationToken cancellationToken)
    {
        if (request.DeletionId.Count == 0)
            return BadRequestError("Form hataydı! Lütfen geliştiricinizle iletişime geçin.");

        await _write.DeleteInvoiceItemsAsync(request.DeletionId, cancellationToken);

        return OkMessage("Kayıt Başarıyla Silindi");
    }
}

/// <summary>
/// olsold: <c>Front\Expedition\ExpeditionController</c> — Sefer (pozisyon).
/// Okuma tarafı; yazma ve Siber senkronu henüz portlanmadı.
/// </summary>
[Authorize]
[Route("api/v1/expedition")]
public sealed class ExpeditionController : ApiControllerBase
{
    private readonly IExpeditionService _expeditions;
    private readonly IExpeditionWriteService _write;
    private readonly IMovementService _movements;
    private readonly ICurrentUser _currentUser;

    public ExpeditionController(
        IExpeditionService expeditions,
        IExpeditionWriteService write,
        IMovementService movements,
        ICurrentUser currentUser)
    {
        _expeditions = expeditions;
        _write = write;
        _movements = movements;
        _currentUser = currentUser;
    }

    [HttpGet]
    [RequiresPermission(PermissionAction.Read, "expedition_management")]
    public async Task<IActionResult> All(
        [FromQuery] string? search,
        [FromQuery(Name = "work_type_id")] int? workTypeId,
        [FromQuery(Name = "date_from")] DateOnly? dateFrom,
        [FromQuery(Name = "date_to")] DateOnly? dateTo,
        [FromQuery(Name = "per_page")] int? perPage,
        [FromQuery] int page = 1,
        CancellationToken cancellationToken = default)
    {
        var result = await _expeditions.ListAsync(
            new ExpeditionListQuery(search, workTypeId, dateFrom, dateTo, perPage, page, CurrentPath),
            cancellationToken);

        return Ok(result, "Kayıtlar");
    }

    [HttpGet("{id:long}")]
    [RequiresPermission(PermissionAction.Read, "expedition_management")]
    public async Task<IActionResult> Single(long id, CancellationToken cancellationToken)
    {
        var expedition = await _expeditions.SingleAsync(id, cancellationToken);

        return expedition is null ? NotFoundError() : Ok(expedition, "Kayıtlar");
    }

    /// <summary>
    /// Sefer hareketleri. Yanıt zarfı diğer uçlardan FARKLI:
    /// <c>{status, message, data, deleted_movements}</c> — arayüz
    /// (<c>ExpeditionFormMovements.vue</c>) kökten <c>data</c> ve
    /// <c>deleted_movements</c> okuyor.
    /// </summary>
    [HttpGet("{id:long}/movements")]
    [RequiresPermission(PermissionAction.Read, "expedition_management")]
    public async Task<IActionResult> Movements(
        long id,
        [FromQuery(Name = "destination_id")] long? destinationId,
        [FromQuery(Name = "expedition_status_id")] long? expeditionStatusId,
        CancellationToken cancellationToken = default)
    {
        var result = await _movements.ExpeditionMovementsAsync(
            id, destinationId, expeditionStatusId, cancellationToken);

        return base.Ok(new Dictionary<string, object?>
        {
            ["status"] = true,
            ["message"] = "Sefer hareketleri başarıyla listelendi",
            ["data"] = result.Data,
            ["deleted_movements"] = result.DeletedMovements,
        });
    }

    /// <summary>
    /// Sefer hareketi ekler ve sefere bağlı HER yük için ayrıca bir yük
    /// hareketi üretir (<c>expedition_movement_id</c> ile eşlenir).
    /// Arayüz <c>FormData</c> gönderiyor.
    ///
    /// Rota <c>{id}</c> taşır ama kaynak sefer kimliğini GÖVDEDEN okuyor;
    /// aynı davranış korundu, gövde boşsa rota parametresine düşülür.
    /// </summary>
    [HttpPost("{id:long}/movements")]
    [RequiresPermission(PermissionAction.Create, "expedition_management")]
    public async Task<IActionResult> SaveMovement(
        long id,
        [FromForm] ExpeditionMovementForm form,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();

        if (form.DestinationId is null)
            errors["destination_id"] = [Translator.Get("Zorunlu Alan")];

        if (form.ExpeditionStatusId is null)
            errors["expedition_status_id"] = [Translator.Get("Zorunlu Alan")];

        if (errors.Count > 0)
            return UnprocessableEntity(new Dictionary<string, object?>
            {
                ["status"] = false,
                ["message"] = "Validasyon hatası",
                ["errors"] = errors,
            });

        var result = await _movements.SaveExpeditionMovementAsync(
            new ExpeditionMovementInput
            {
                ExpeditionId = form.ExpeditionId ?? id,
                DestinationId = form.DestinationId,
                ExpeditionStatusId = form.ExpeditionStatusId,
                Description = form.Description,
                Address = form.Address,
            },
            _currentUser.Id ?? 0,
            cancellationToken);

        if (!result.IsSuccess)
            return NotFound(new Dictionary<string, object?>
            {
                ["status"] = false,
                ["message"] = result.NotFoundMessage,
            });

        return StatusCode(StatusCodes.Status201Created, new Dictionary<string, object?>
        {
            ["status"] = true,
            ["message"] = "Sefer hareketi başarıyla oluşturuldu",
            ["data"] = result.Data,
        });
    }

    /// <summary>
    /// Soft delete. Kaynak kayıt bulunmasa da başarı döndürüyor — korundu.
    /// </summary>
    [HttpDelete("{id:long}/movements/{movementId:long}")]
    [RequiresPermission(PermissionAction.Delete, "expedition_management")]
    public async Task<IActionResult> DeleteMovement(
        long id, long movementId, CancellationToken cancellationToken)
    {
        await _movements.DeleteExpeditionMovementAsync(id, movementId, cancellationToken);

        return base.Ok(new Dictionary<string, object?>
        {
            ["status"] = true,
            ["message"] = "Sefer hareketi başarıyla silindi",
        });
    }

    public sealed class ExpeditionMovementForm
    {
        [FromForm(Name = "expedition_id")] public long? ExpeditionId { get; set; }
        [FromForm(Name = "destination_id")] public long? DestinationId { get; set; }
        [FromForm(Name = "expedition_status_id")] public long? ExpeditionStatusId { get; set; }
        [FromForm(Name = "description")] public string? Description { get; set; }
        [FromForm(Name = "address")] public string? Address { get; set; }
    }

    public sealed class ExpeditionRequest
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("romork_id")] public long? RomorkId { get; set; }
        [JsonPropertyName("work_type")] public long? WorkType { get; set; }
        [JsonPropertyName("department_id")] public long? DepartmentId { get; set; }
        [JsonPropertyName("expedition_type")] public long? ExpeditionType { get; set; }
        [JsonPropertyName("expedition_type_id")] public long? ExpeditionTypeId { get; set; }
        [JsonPropertyName("expedition_status_id")] public long? ExpeditionStatusId { get; set; }
        [JsonPropertyName("release_date")] public DateOnly? ReleaseDate { get; set; }
        [JsonPropertyName("entry_date")] public DateOnly? EntryDate { get; set; }
        [JsonPropertyName("loading_date")] public DateOnly? LoadingDate { get; set; }
        [JsonPropertyName("return_date")] public DateOnly? ReturnDate { get; set; }
        [JsonPropertyName("car_exit_date")] public DateOnly? CarExitDate { get; set; }
        [JsonPropertyName("start_city_id")] public Guid? StartCityId { get; set; }
        [JsonPropertyName("load_city_id")] public Guid? LoadCityId { get; set; }
        [JsonPropertyName("end_city_id")] public Guid? EndCityId { get; set; }

        public ExpeditionWriteModel ToModel(long? currentUserId) => new()
        {
            Id = Id,
            RomorkId = RomorkId,
            WorkType = WorkType,
            DepartmentId = DepartmentId,
            // Kaynakta save 'expedition_type', update 'expedition_type_id' okuyor.
            ExpeditionTypeId = ExpeditionTypeId ?? ExpeditionType,
            ExpeditionStatusId = ExpeditionStatusId,
            ReleaseDate = ReleaseDate,
            EntryDate = EntryDate,
            LoadingDate = LoadingDate,
            ReturnDate = ReturnDate,
            CarExitDate = CarExitDate,
            StartCityId = StartCityId,
            LoadCityId = LoadCityId,
            EndCityId = EndCityId,
            CurrentUserId = currentUserId,
        };
    }

    [HttpPost]
    [RequiresPermission(PermissionAction.Create, "expedition_management")]
    public async Task<IActionResult> Save(
        [FromBody] ExpeditionRequest request, CancellationToken cancellationToken)
    {
        if (Validate(request, isUpdate: false) is { } errors)
            return BadRequest(ApiResponse.ValidationErrors(errors));

        var result = await _write.CreateAsync(
            request.ToModel(_currentUser.Id), cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(ApiResponse.Error(result.ErrorMessage!));

        var expedition = await _expeditions.SingleAsync(result.Id!.Value, cancellationToken);
        return Ok(expedition, "Kayıt Başarılı");
    }

    [HttpPut]
    [RequiresPermission(PermissionAction.Update, "expedition_management")]
    public async Task<IActionResult> Update(
        [FromBody] ExpeditionRequest request, CancellationToken cancellationToken)
    {
        if (request.Id is null)
            return BadRequestError("Form hataydı! Lütfen geliştiricinizle iletişime geçin.");

        if (Validate(request, isUpdate: true) is { } errors)
            return BadRequest(ApiResponse.ValidationErrors(errors));

        var result = await _write.UpdateAsync(
            request.ToModel(_currentUser.Id), cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(ApiResponse.Error(result.ErrorMessage!));

        var expedition = await _expeditions.SingleAsync(result.Id!.Value, cancellationToken);
        return Ok(expedition, "Güncelleme Başarılı");
    }

    /// <summary>olsold: <c>expeditionUpdate</c>'te <c>expedition_status_id == 8</c> ise devreye giren blok.</summary>
    private const int RequiresFullDetailsStatusId = 8;

    /// <summary>
    /// olsold: <c>expeditionSave</c>/<c>expeditionUpdate</c> FormRequest kuralları. Temel alanlar
    /// (romork/tip/departman) ikisinde de aynı; <c>expedition_status_id</c> ve durum=8 koşullu bloğu
    /// (tarihler + 3 şehir) yalnızca Update'te var. Tarih sırası kontrolü (<c>after_or_equal</c>)
    /// durumdan bağımsız — ikisi de doluysa her zaman uygulanır.
    /// </summary>
    private Dictionary<string, string[]>? Validate(ExpeditionRequest request, bool isUpdate)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.RomorkId is null) errors["romork_id"] = [Translator.Get("Romork Zorunludur")];
        if ((request.ExpeditionTypeId ?? request.ExpeditionType) is null)
            errors["expedition_type"] = [Translator.Get("Sefer Tipi Zorunludur")];
        if (request.WorkType is null) errors["work_type"] = [Translator.Get("Çalışma Tipi Zorunludur")];
        if (request.DepartmentId is null) errors["department_id"] = [Translator.Get("Departman Zorunludur")];

        if (isUpdate)
        {
            if (request.ExpeditionStatusId is null)
                errors["expedition_status_id"] = [Translator.Get("Sefer Durumu Zorunludur")];

            if (request.ExpeditionStatusId == RequiresFullDetailsStatusId)
            {
                if (request.CarExitDate is null) errors["car_exit_date"] = [Translator.Get("Araç Çıkış Tarihi Zorunludur")];
                if (request.ReleaseDate is null) errors["release_date"] = [Translator.Get("Başlangıç Tarihi Zorunludur")];
                if (request.ReturnDate is null) errors["return_date"] = [Translator.Get("Bitiş Tarihi Zorunludur")];
                if (request.LoadingDate is null) errors["loading_date"] = [Translator.Get("Yükleme Tarihi Zorunludur")];
                if (request.StartCityId is null) errors["start_city_id"] = [Translator.Get("Başlangıç Şehri Zorunludur")];
                if (request.LoadCityId is null) errors["load_city_id"] = [Translator.Get("Yükleme Şehri Zorunludur")];
                if (request.EndCityId is null) errors["end_city_id"] = [Translator.Get("Bitiş Şehri Zorunludur")];
            }
        }

        // olsold: return_date/loading_date >= release_date — ikisi de doluysa, durumdan bağımsız kontrol edilir.
        if (request.ReleaseDate is { } release)
        {
            if (request.ReturnDate is { } returnDate && returnDate < release && !errors.ContainsKey("return_date"))
                errors["return_date"] = [Translator.Get("Bitiş Tarihi Başlangıç tarihinden küçük olamaz")];
            if (request.LoadingDate is { } loadingDate && loadingDate < release && !errors.ContainsKey("loading_date"))
                errors["loading_date"] = [Translator.Get("Yükleme Tarihi Başlangıç tarihinden küçük olamaz")];
        }

        return errors.Count > 0 ? errors : null;
    }

    [HttpDelete]
    [RequiresPermission(PermissionAction.Delete, "expedition_management")]
    public async Task<IActionResult> Delete(
        [FromBody] DeletionRequest request, CancellationToken cancellationToken)
    {
        if (request.DeletionId.Count == 0)
            return BadRequestError("Form hataydı! Lütfen geliştiricinizle iletişime geçin.");

        await _write.DeleteAsync(request.DeletionId, cancellationToken);

        return OkMessage("Kayıt Başarıyla Silindi");
    }
}
