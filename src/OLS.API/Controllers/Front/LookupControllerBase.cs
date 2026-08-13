using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OLS.Business.Common;
using OLS.Business.Services.Authorization;
using OLS.Business.Services.Lookups;

namespace OLS.API.Controllers.Front;

/// <summary>
/// Referans/tanım modüllerinin ortak controller'ı.
///
/// olsold'da bu beş metot 27 controller'da birebir kopyalanmıştı. Uç imzaları
/// korunuyor (frontend bunlara bağlı):
///
///   POST   /api/v1/&lt;kaynak&gt;          save
///   PUT    /api/v1/&lt;kaynak&gt;          update
///   POST   /api/v1/&lt;kaynak&gt;/update   update  (bazı modüller bunu kullanıyor)
///   DELETE /api/v1/&lt;kaynak&gt;          delete  (gövdede deletion_id)
///   GET    /api/v1/&lt;kaynak&gt;          all     (?search= ILIKE, ?per_page=)
///   GET    /api/v1/&lt;kaynak&gt;/{id}     single
///
/// Kaynakta modüllerin bir kısmı PUT, bir kısmı POST /update kullanıyordu;
/// ikisini de açıyoruz ki hangisi çağrılırsa çağrılsın çalışsın.
/// </summary>
[Authorize]
public abstract class LookupControllerBase<TEntity> : ApiControllerBase where TEntity : class
{
    private readonly ILookupService<TEntity> _service;

    protected LookupControllerBase(ILookupService<TEntity> service) => _service = service;

    /// <summary>RoleHelper slug'ı. Kaynak koddaki değerle birebir aynı olmalı.</summary>
    protected abstract string PermissionSlug { get; }

    /// <summary>olsold'daki orderBy yönü. Çoğu modül id desc, birkaçı asc.</summary>
    protected virtual bool OrderAscending => false;

    [HttpGet]
    public async Task<IActionResult> All(
        [FromQuery] string? search,
        [FromQuery(Name = "per_page")] int? perPage,
        [FromQuery] int? type,
        [FromQuery] int page = 1,
        CancellationToken cancellationToken = default)
    {
        if (!await HasPermissionAsync(PermissionAction.Read, cancellationToken))
            return Forbidden();

        var result = await _service.AllAsync(
            search, perPage, page, CurrentPath, OrderAscending, type, cancellationToken);

        return Ok(result, "Kayıtlar");
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Single(long id, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(PermissionAction.Read, cancellationToken))
            return Forbidden();

        var entity = await _service.SingleAsync(id, cancellationToken);

        return entity is null ? NotFoundError() : Ok(entity, "Kayıtlar");
    }

    [HttpPost]
    public async Task<IActionResult> Save(
        [FromBody] Dictionary<string, JsonElement> body, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(PermissionAction.Create, cancellationToken))
            return Forbidden();

        var entity = await _service.CreateAsync(body, cancellationToken);

        return Ok(entity, "Kayıt Başarılı");
    }

    [HttpPut]
    public Task<IActionResult> UpdateViaPut(
        [FromBody] Dictionary<string, JsonElement> body, CancellationToken cancellationToken) =>
        UpdateInternal(body, cancellationToken);

    [HttpPost("update")]
    public Task<IActionResult> UpdateViaPost(
        [FromBody] Dictionary<string, JsonElement> body, CancellationToken cancellationToken) =>
        UpdateInternal(body, cancellationToken);

    [HttpDelete]
    public async Task<IActionResult> Delete(
        [FromBody] DeletionRequest request, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(PermissionAction.Delete, cancellationToken))
            return Forbidden();

        if (request.DeletionId.Count == 0)
            return BadRequestError("Form hataydı! Lütfen geliştiricinizle iletişime geçin.");

        await _service.DeleteAsync(request.DeletionId, cancellationToken);

        return OkMessage("Kayıt Başarıyla Silindi");
    }

    private async Task<IActionResult> UpdateInternal(
        Dictionary<string, JsonElement> body, CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(PermissionAction.Update, cancellationToken))
            return Forbidden();

        // olsold id'yi route'tan değil gövdeden alıyordu.
        if (!body.TryGetValue("id", out var idElement))
            return BadRequest(ApiResponse.ValidationErrors(new Dictionary<string, string[]>
            {
                ["id"] = [Translator.Get("Bu alan boş bırakılamaz")],
            }));

        var id = idElement.ValueKind == JsonValueKind.String
            ? long.TryParse(idElement.GetString(), out var parsed) ? parsed : 0
            : idElement.TryGetInt64(out var direct) ? direct : 0;

        if (id == 0)
            return BadRequestError("Form hataydı! Lütfen geliştiricinizle iletişime geçin.");

        var entity = await _service.UpdateAsync(id, body, cancellationToken);

        return entity is null ? NotFoundError() : Ok(entity, "Güncelleme Başarılı");
    }

    private async Task<bool> HasPermissionAsync(
        PermissionAction action, CancellationToken cancellationToken)
    {
        var currentUser = HttpContext.RequestServices.GetRequiredService<ICurrentUser>();
        if (currentUser.Id is not { } userId)
            return false;

        var permissions = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
        return await permissions.HasPermissionAsync(userId, PermissionSlug, action, cancellationToken);
    }

    private IActionResult Forbidden() =>
        StatusCode(StatusCodes.Status403Forbidden,
            ApiResponse.Error(Translator.Get("Yetkisiz Erişim")));
}
