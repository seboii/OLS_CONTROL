using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OLS.API.Filters;
using OLS.API.Services;
using OLS.Business.Common;
using OLS.Business.Services.Accounts;
using OLS.Business.Services.Authorization;

namespace OLS.API.Controllers.Front;

/// <summary>
/// olsold: <c>Front\Account\FrontAccountController</c> — Cari yönetimi.
///
/// Uç imzaları olsold ile aynı tutuldu (frontend bunlara bağlı):
///   POST   /api/v1/account          save
///   POST   /api/v1/account/update   update  (id gövdede)
///   DELETE /api/v1/account          delete  (gövdede deletion_id dizisi)
///   GET    /api/v1/account          all
///   GET    /api/v1/account/{id}     single
/// </summary>
[Authorize]
[Route("api/v1/account")]
public sealed class AccountController : ApiControllerBase
{
    private readonly IAccountService _accounts;
    private readonly ICurrentUser _currentUser;
    private readonly IFileStorage _files;

    public AccountController(
        IAccountService accounts, ICurrentUser currentUser, IFileStorage files)
    {
        _accounts = accounts;
        _currentUser = currentUser;
        _files = files;
    }

    [HttpGet]
    [RequiresPermission(PermissionAction.Read, "account_management")]
    public async Task<IActionResult> All(
        [FromQuery] string? search,
        [FromQuery(Name = "account_type_id")] long? accountTypeId,
        [FromQuery(Name = "per_page")] int? perPage,
        [FromQuery] int page = 1,
        CancellationToken cancellationToken = default)
    {
        if (_currentUser.Id is not { } userId)
            return Unauthorized(ApiResponse.Error(Translator.Get("Yetkisiz Erişim")));

        var result = await _accounts.ListAsync(
            new AccountListQuery(userId, search, accountTypeId, perPage, page, CurrentPath),
            cancellationToken);

        return Ok(result, "Kayıtlar");
    }

    [HttpGet("{id:long}")]
    [RequiresPermission(PermissionAction.Read, "account_management")]
    public async Task<IActionResult> Single(long id, CancellationToken cancellationToken)
    {
        if (_currentUser.Id is not { } userId)
            return Unauthorized(ApiResponse.Error(Translator.Get("Yetkisiz Erişim")));

        // olsold: süper admin her cariyi görür; değilse yalnızca kendisine
        // atanmış cariler, aksi halde 403.
        if (!await _accounts.IsVisibleToUserAsync(userId, id, cancellationToken))
            return StatusCode(StatusCodes.Status403Forbidden,
                ApiResponse.Message(Translator.Get("Yetkisiz Erişim")));

        var account = await _accounts.SingleAsync(id, cancellationToken);

        return account is null
            ? NotFoundError()
            : Ok(account, "Kayıtlar");
    }

    [HttpPost]
    [RequiresPermission(PermissionAction.Create, "account_management")]
    public async Task<IActionResult> Save(
        [FromForm] AccountFormRequest form, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(form.Name))
            return BadRequest(ApiResponse.ValidationErrors(new Dictionary<string, string[]>
            {
                ["name"] = [Translator.Get("Bu alan boş bırakılamaz")],
            }));

        var avatar = await _files.SaveAvatarAsync(form.Avatar, cancellationToken);

        var result = await _accounts.CreateAsync(
            form.ToWriteModel(avatar), cancellationToken);

        return BuildSaveResponse(result, "Kayıt Başarılı");
    }

    [HttpPost("update")]
    [RequiresPermission(PermissionAction.Update, "account_management")]
    public async Task<IActionResult> Update(
        [FromForm] AccountFormRequest form, CancellationToken cancellationToken)
    {
        if (form.Id is null)
            return BadRequest(ApiResponse.ValidationErrors(new Dictionary<string, string[]>
            {
                ["id"] = [Translator.Get("Bu alan boş bırakılamaz")],
            }));

        var avatar = await _files.SaveAvatarAsync(form.Avatar, cancellationToken);

        var result = await _accounts.UpdateAsync(
            form.ToWriteModel(avatar), cancellationToken);

        return BuildSaveResponse(result, "Güncelleme Başarılı");
    }

    [HttpDelete]
    [RequiresPermission(PermissionAction.Delete, "account_management")]
    public async Task<IActionResult> Delete(
        [FromBody] DeletionRequest request, CancellationToken cancellationToken)
    {
        if (request.DeletionId.Count == 0)
            return BadRequestError("Form hataydı! Lütfen geliştiricinizle iletişime geçin.");

        await _accounts.DeleteAsync(request.DeletionId, cancellationToken);

        return OkMessage("Kayıt Başarıyla Silindi");
    }

    /// <summary>
    /// olsold ad/e-posta çakışmasında 500 + <c>{errors:{alan:[mesaj]}}</c> dönüyordu.
    /// Durum kodunu 422'ye çektik (çakışma sunucu hatası değil, doğrulama hatası);
    /// zarf şekli aynı kaldığı için frontend'in hata gösterimi etkilenmiyor.
    /// </summary>
    private IActionResult BuildSaveResponse(AccountSaveResult result, string successKey)
    {
        if (result.IsSuccess)
            return Ok(result.Account, successKey);

        var message = result.DuplicateField == "name"
            ? Translator.Get("Aynı isimde kayıt bulunmaktadır.")
            : Translator.Get("Bu email adresi ile kayıt bulunmaktadır.");

        return UnprocessableEntity(ApiResponse.ValidationErrors(new Dictionary<string, string[]>
        {
            [result.DuplicateField!] = [message],
        }));
    }
}
