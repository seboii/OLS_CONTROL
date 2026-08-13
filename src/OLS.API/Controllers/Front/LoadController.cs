using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OLS.API.Filters;
using OLS.API.Services;
using OLS.Business.Common;
using OLS.Business.Services.Authorization;
using OLS.Business.Services.Loads;

namespace OLS.API.Controllers.Front;

/// <summary>
/// olsold: <c>Front\Load\LoadController</c> — Yük / teklif yönetimi.
///
/// Dikkat: <c>Load</c> hem yükü hem TEKLİFİ temsil eder
/// (<c>status_type_id = 4</c> teklif demektir).
///
///   GET    /api/v1/load                     all
///   GET    /api/v1/load/{id}                single
///   POST   /api/v1/load                     save
///   POST   /api/v1/load/{id}                update
///   DELETE /api/v1/load                     delete (gövdede deletion_id)
///   DELETE /api/v1/load/load_content        içerik satırı silme
///   DELETE /api/v1/load/load_financial_item finansal kalem silme
///   POST   /api/v1/load/updateTimeOut       updateTimeOut
///
/// saveAi (AI çıktısından teklif oluşturma) ve Siber rezervasyon senkronu
/// (transfer_to_siber) henüz portlanmadı.
/// </summary>
[Authorize]
[Route("api/v1/load")]
public sealed class LoadController : ApiControllerBase
{
    private readonly ILoadService _loads;
    private readonly ILoadWriteService _write;
    private readonly ICurrentUser _currentUser;
    private readonly IFileStorage _files;

    public LoadController(
        ILoadService loads,
        ILoadWriteService write,
        ICurrentUser currentUser,
        IFileStorage files)
    {
        _loads = loads;
        _write = write;
        _currentUser = currentUser;
        _files = files;
    }

    [HttpGet]
    [RequiresPermission(PermissionAction.Read, "load_management")]
    public async Task<IActionResult> All(
        [FromQuery] string? search,
        [FromQuery(Name = "status_type_id")] int? statusTypeId,
        [FromQuery] int? timeout,
        [FromQuery(Name = "per_page")] int? perPage,
        [FromQuery] int page = 1,
        CancellationToken cancellationToken = default)
    {
        if (_currentUser.Id is not { } userId)
            return Unauthorized(ApiResponse.Error(Translator.Get("Yetkisiz Erişim")));

        var result = await _loads.ListAsync(
            new LoadListQuery(userId, search, statusTypeId, timeout is 1, perPage, page, CurrentPath),
            cancellationToken);

        return Ok(result, "Kayıtlar");
    }

    [HttpGet("{id:long}")]
    [RequiresPermission(PermissionAction.Read, "load_management")]
    public async Task<IActionResult> Single(long id, CancellationToken cancellationToken)
    {
        var load = await _loads.SingleAsync(id, cancellationToken);

        return load is null
            ? NotFoundError()
            : Ok(load, "Kayıtlar");
    }

    /// <summary>
    /// olsold: <c>POST /load/saveAi</c> — AI çıktısından teklif oluşturur.
    ///
    /// OpenAI çağrısı arayüzde yapılır (<c>Offer.vue</c>); bu uç yalnızca
    /// hazır JSON'u alıp kaydeder, dış servise dokunmaz.
    ///
    /// AI id değil <b>ad</b> döndürdüğü için iş tipi / ödeme tipi / ülke /
    /// ürün tipi / para birimi adla eşlenir. Eşleşmeyen alanlar boş kalır ve
    /// yanıtta <c>eslesmeyen</c> listesinde bildirilir — kaynak sessizce
    /// geçiyordu, kullanıcı hangi alanın boş kaldığını göremiyordu.
    /// </summary>
    [HttpPost("saveAi")]
    [RequiresPermission(PermissionAction.Create, "load_management")]
    public async Task<IActionResult> SaveAi(
        [FromBody] LoadAiRequest request,
        [FromServices] ILoadAiImportService ai,
        CancellationToken cancellationToken)
    {
        var result = await ai.CreateAsync(request, cancellationToken);

        return base.Ok(ApiResponse.Success(
            new { id = result.LoadId, eslesmeyen = result.Unresolved },
            "Kayıt Başarılı"));
    }

    [HttpDelete]
    [RequiresPermission(PermissionAction.Delete, "load_management")]
    public async Task<IActionResult> Delete(
        [FromBody] DeletionRequest request, CancellationToken cancellationToken)
    {
        if (request.DeletionId.Count == 0)
            return BadRequestError("Form hataydı! Lütfen geliştiricinizle iletişime geçin.");

        var result = await _loads.DeleteAsync(request.DeletionId, cancellationToken);

        // olsold'un mesajı birebir korunuyor (frontend bu metni gösteriyor).
        if (!result.Success)
            return BadRequest(ApiResponse.ValidationErrors(new Dictionary<string, string[]>
            {
                ["message"] = ["Yük oluşturulmuş kayıt silinemez"],
            }));

        return OkMessage("Kayıt Başarıyla Silindi");
    }

    public sealed class UpdateTimeOutRequest
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }
    }

    [HttpPost("updateTimeOut")]
    [RequiresPermission(PermissionAction.Update, "load_management")]
    public async Task<IActionResult> UpdateTimeOut(
        [FromBody] UpdateTimeOutRequest request, CancellationToken cancellationToken)
    {
        var ok = await _loads.UpdateTimeOutAsync(request.Id, cancellationToken);

        return ok
            ? OkMessage("Güncelleme Başarılı")
            : NotFoundError();
    }

    [HttpPost]
    [RequiresPermission(PermissionAction.Create, "load_management")]
    public async Task<IActionResult> Save(
        [FromForm] LoadFormRequest form, CancellationToken cancellationToken)
    {
        if (_currentUser.Id is not { } userId)
            return Unauthorized(ApiResponse.Error(Translator.Get("Yetkisiz Erişim")));

        if (Validate(form) is { } errors)
            return BadRequest(ApiResponse.ValidationErrors(errors));

        var uploaded = await SaveFilesAsync(form, cancellationToken);
        var id = await _write.CreateAsync(form.ToModel(userId, uploaded), cancellationToken);

        var load = await _loads.SingleAsync(id, cancellationToken);
        return Ok(load, "Kayıt Başarılı");
    }

    /// <summary>olsold rotası: <c>POST /api/v1/load/{id?}</c> güncelleme içindi.</summary>
    [HttpPost("{id:long}")]
    [RequiresPermission(PermissionAction.Update, "load_management")]
    public async Task<IActionResult> Update(
        long id, [FromForm] LoadFormRequest form, CancellationToken cancellationToken)
    {
        if (_currentUser.Id is not { } userId)
            return Unauthorized(ApiResponse.Error(Translator.Get("Yetkisiz Erişim")));

        if (Validate(form) is { } errors)
            return BadRequest(ApiResponse.ValidationErrors(errors));

        var uploaded = await SaveFilesAsync(form, cancellationToken);

        var model = form.ToModel(userId, uploaded) with { Id = id };
        var result = await _write.UpdateAsync(model, cancellationToken);

        if (result.Id is null)
            return NotFoundError();

        // Kaydedilen kaldırılmış dosyaların DB satırı LoadWriteService'te silindi;
        // fiziksel dosya OLS.Business'ın erişemediği IFileStorage'a bağımlı olduğu
        // için burada, çağıran katmanda silinir (bkz. LoadUpdateResult).
        foreach (var name in result.RemovedFileNames)
            _files.Delete(name);

        var load = await _loads.SingleAsync(result.Id.Value, cancellationToken);
        return Ok(load, "Güncelleme Başarılı");
    }

    [HttpDelete("load_content")]
    [RequiresPermission(PermissionAction.Delete, "load_management")]
    public async Task<IActionResult> DeleteContents(
        [FromBody] DeletionRequest request, CancellationToken cancellationToken)
    {
        if (request.DeletionId.Count == 0)
            return BadRequestError("Form hataydı! Lütfen geliştiricinizle iletişime geçin.");

        await _write.DeleteContentsAsync(request.DeletionId, cancellationToken);
        return OkMessage("Kayıt Başarıyla Silindi");
    }

    [HttpDelete("load_financial_item")]
    [RequiresPermission(PermissionAction.Delete, "load_management")]
    public async Task<IActionResult> DeleteFinancialItems(
        [FromBody] DeletionRequest request, CancellationToken cancellationToken)
    {
        if (request.DeletionId.Count == 0)
            return BadRequestError("Form hataydı! Lütfen geliştiricinizle iletişime geçin.");

        await _write.DeleteFinancialItemsAsync(request.DeletionId, cancellationToken);
        return OkMessage("Kayıt Başarıyla Silindi");
    }

    /// <summary>olsold: LoadSave FormRequest kuralları.</summary>
    private Dictionary<string, string[]>? Validate(LoadFormRequest form)
    {
        var errors = new Dictionary<string, string[]>();
        var required = Translator.Get("Bu alan boş bırakılamaz");

        if (form.WorkTypeId is null) errors["work_type_id"] = [required];
        if (form.LoadingTypeId is null) errors["loading_type_id"] = [required];
        if (form.PaymentTypeId is null) errors["payment_type_id"] = [required];
        if (form.StatusTypeId is null) errors["status_type_id"] = [required];
        if (form.OfferDate is null) errors["offer_date"] = [required];
        if (form.OfferValidityDate is null) errors["offer_validity_date"] = [required];
        if (form.MarketingNotificationDate is null) errors["marketing_notification_date"] = [required];
        if (form.CustomerId is null) errors["customer_id"] = [required];
        if (form.DepartmentId is null) errors["department_id"] = [required];
        if (form.LoadContent.Count == 0) errors["load_content"] = [required];

        return errors.Count > 0 ? errors : null;
    }

    private async Task<List<UploadedFile>> SaveFilesAsync(
        LoadFormRequest form, CancellationToken cancellationToken)
    {
        var uploaded = new List<UploadedFile>();

        foreach (var file in form.Files)
        {
            var stored = await _files.SaveDocumentAsync(file, cancellationToken);
            if (stored is not null)
            {
                uploaded.Add(new UploadedFile(
                    stored, Path.GetExtension(file.FileName).TrimStart('.'), file.FileName));
            }
        }

        return uploaded;
    }
}
