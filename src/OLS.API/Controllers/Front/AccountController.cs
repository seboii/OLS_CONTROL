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

    /// <summary>
    /// <c>GET /api/v1/account/{id}/representatives</c> — cariye bağlı varsayılan
    /// görevliler. Teklif formunda müşteri seçilince "Görevliler" sekmesi bununla
    /// kendiliğinden dolar (kullanıcı isteği).
    /// </summary>
    [HttpGet("{id:long}/representatives")]
    [RequiresPermission(PermissionAction.Read, "account_management")]
    public async Task<IActionResult> Representatives(long id, CancellationToken cancellationToken)
    {
        var result = await _accounts.RepresentativesAsync(id, cancellationToken);

        return Ok(result, "Kayıtlar");
    }

    [HttpGet]
    [RequiresPermission(PermissionAction.Read, "account_management")]
    public async Task<IActionResult> All(
        [FromQuery] string? search,
        [FromQuery(Name = "account_type_id")] long? accountTypeId,
        [FromQuery(Name = "per_page")] int? perPage,
        [FromQuery] int page = 1,
        [FromQuery(Name = "country_id")] Guid? countryId = null,
        [FromQuery(Name = "tax_office_id")] long? taxOfficeId = null,
        [FromQuery(Name = "assigned_user_id")] int? assignedUserId = null,
        [FromQuery(Name = "individual_personal")] string? individualPersonal = null,
        CancellationToken cancellationToken = default)
    {
        if (_currentUser.Id is not { } userId)
            return Unauthorized(ApiResponse.Error(Translator.Get("Yetkisiz Erişim")));

        var result = await _accounts.ListAsync(
            new AccountListQuery(
                userId, search, accountTypeId, perPage, page, CurrentPath,
                countryId, taxOfficeId, assignedUserId, individualPersonal),
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
        var isSuperAdmin = await _accounts.IsSuperAdminAsync(userId, cancellationToken);
        if (!isSuperAdmin && !await _accounts.IsVisibleToUserAsync(userId, id, cancellationToken))
            return StatusCode(StatusCodes.Status403Forbidden,
                ApiResponse.Message(Translator.Get("Yetkisiz Erişim")));

        // olsold: 'Invoice' ilişkisi single()'da yalnızca süper admin dalında yükleniyordu.
        var account = await _accounts.SingleAsync(id, isSuperAdmin, cancellationToken);

        return account is null
            ? NotFoundError()
            : Ok(account, "Kayıtlar");
    }

    [HttpPost]
    // OLUŞTURMA YETKİYE BAĞLI DEĞİL. Müşteri / araç / teklif / yük / sefer
    // kaydı açmak herkese açık; okuma, güncelleme ve silme yetkileri
    // olduğu gibi duruyor (arayüzde de aynı, bkz. canCreate).
    public async Task<IActionResult> Save(
        [FromForm] AccountFormRequest form, CancellationToken cancellationToken)
    {
        if (Validate(form) is { } errors)
            return BadRequest(ApiResponse.ValidationErrors(errors));

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

        if (Validate(form) is { } errors)
            return BadRequest(ApiResponse.ValidationErrors(errors));

        var avatar = await _files.SaveAvatarAsync(form.Avatar, cancellationToken);

        var result = await _accounts.UpdateAsync(
            form.ToWriteModel(avatar), cancellationToken);

        return BuildSaveResponse(result, "Güncelleme Başarılı");
    }

    /// <summary>olsold: <c>RequestSave</c>/<c>RequestUpdate</c> — ikisinde de birebir aynı 3 kural.</summary>
    private Dictionary<string, string[]>? Validate(AccountFormRequest form)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(form.Name))
            errors["name"] = [Translator.Get("Adı boş olamaz")];
        if (form.CountryId is null)
            errors["country_id"] = [Translator.Get("Ülke seçimi yapılmalıdır")];
        if (form.Discount is null)
            errors["discount"] = [Translator.Get("İndirim oranı boş olamaz")];

        return errors.Count > 0 ? errors : null;
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
