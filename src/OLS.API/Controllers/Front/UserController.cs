using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OLS.API.Filters;
using OLS.API.Services;
using OLS.Business.Common;
using OLS.Business.Services.Authorization;
using OLS.Business.Services.Users;

namespace OLS.API.Controllers.Front;

/// <summary>
/// olsold: <c>Front\FrontUser\FrontUserController</c>
///
///   GET    /api/v1/user            (?search=&amp;working_tracking=&amp;per_page=)
///   GET    /api/v1/user/{id}
///   POST   /api/v1/user            (multipart — avatar)
///   POST   /api/v1/user/update
///   DELETE /api/v1/user            (toplu: deletion_id[])
/// </summary>
[Authorize]
[Route("api/v1/user")]
public sealed class UserController : ApiControllerBase
{
    private const string PermissionSlug = "user_management";

    private readonly IUserService _users;
    private readonly IFileStorage _files;

    public UserController(IUserService users, IFileStorage files)
    {
        _users = users;
        _files = files;
    }

    [HttpGet]
    [RequiresPermission(PermissionAction.Read, PermissionSlug)]
    public async Task<IActionResult> Index(
        [FromQuery] string? search,
        [FromQuery(Name = "working_tracking")] bool? workingTracking,
        [FromQuery(Name = "per_page")] int? perPage,
        [FromQuery] int page = 1,
        CancellationToken cancellationToken = default)
    {
        var result = await _users.ListAsync(
            new UserListQuery(search, workingTracking, perPage, page, CurrentPath),
            cancellationToken);

        return Ok(result, "Kayıtlar");
    }

    [HttpGet("{id:long}")]
    [RequiresPermission(PermissionAction.Read, PermissionSlug)]
    public async Task<IActionResult> Single(long id, CancellationToken cancellationToken)
    {
        var user = await _users.SingleAsync(id, cancellationToken);

        return user is null ? NotFoundError() : Ok(user, "Kayıtlar");
    }

    [HttpPost]
    [RequiresPermission(PermissionAction.Create, PermissionSlug)]
    public async Task<IActionResult> Save(
        [FromForm] UserForm form, CancellationToken cancellationToken)
    {
        var errors = await ValidateAsync(form, null, requirePassword: true, cancellationToken);

        if (errors.Count > 0)
            return UnprocessableEntity(ApiResponse.ValidationErrors(errors));

        var avatar = form.Avatar is null
            ? null
            : await _files.SaveAvatarAsync(form.Avatar, cancellationToken);

        var created = await _users.CreateAsync(form.ToRequest(), avatar, cancellationToken);

        return Ok(created, "Kayıt Başarılı");
    }

    [HttpPost("update")]
    [RequiresPermission(PermissionAction.Update, PermissionSlug)]
    public async Task<IActionResult> Update(
        [FromForm] UserUpdateForm form, CancellationToken cancellationToken)
    {
        var errors = await ValidateAsync(form, form.Id, requirePassword: false, cancellationToken);

        if (form.Id is null or <= 0)
            errors["id"] = [Translator.Get("Zorunlu Alan")];

        if (errors.Count > 0)
            return UnprocessableEntity(ApiResponse.ValidationErrors(errors));

        var avatar = form.Avatar is null
            ? null
            : await _files.SaveAvatarAsync(form.Avatar, cancellationToken);

        var updated = await _users.UpdateAsync(
            form.ToUpdateRequest(), avatar, form.AvatarRemove == 1, cancellationToken);

        return updated is null ? NotFoundError() : Ok(updated, "Güncelleme Başarılı");
    }

    [HttpDelete]
    [RequiresPermission(PermissionAction.Delete, PermissionSlug)]
    public async Task<IActionResult> Delete(
        [FromBody] InvoiceController.DeleteRequest request, CancellationToken cancellationToken)
    {
        if (request.DeletionId is not { Count: > 0 })
            return UnprocessableEntity(ApiResponse.ValidationErrors(
                new Dictionary<string, string[]> { ["deletion_id"] = [Translator.Get("Zorunlu Alan")] }));

        var avatars = await _users.DeleteAsync(request.DeletionId, cancellationToken);

        foreach (var avatar in avatars)
            _files.Delete(avatar);

        return base.Ok(ApiResponse.Message("Kayıt Başarıyla Silindi"));
    }

    private async Task<Dictionary<string, string[]>> ValidateAsync(
        UserForm form, long? exceptId, bool requirePassword, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        var required = Translator.Get("Zorunlu Alan");

        if (string.IsNullOrWhiteSpace(form.Name))
            errors["name"] = [required];

        if (string.IsNullOrWhiteSpace(form.Surname))
            errors["surname"] = [required];

        if (string.IsNullOrWhiteSpace(form.Email) || !form.Email.Contains('@'))
            errors["email"] = [required];
        else if (await _users.EmailExistsAsync(form.Email, exceptId, cancellationToken))
            errors["email"] = [Translator.Get("Bu e-posta zaten kayıtlı.")];

        if (requirePassword && string.IsNullOrWhiteSpace(form.Password))
            errors["password"] = [required];

        return errors;
    }

    /// <summary>Form gövdesi — bağlayıcı <c>[JsonPropertyName]</c>'i yok sayar.</summary>
    public class UserForm
    {
        [FromForm(Name = "name")] public string? Name { get; set; }
        [FromForm(Name = "surname")] public string? Surname { get; set; }
        [FromForm(Name = "email")] public string? Email { get; set; }
        [FromForm(Name = "phone")] public string? Phone { get; set; }
        [FromForm(Name = "phone_country_id")] public Guid? PhoneCountryId { get; set; }
        [FromForm(Name = "password")] public string? Password { get; set; }
        [FromForm(Name = "working_tracking")] public bool WorkingTracking { get; set; }
        [FromForm(Name = "pkds_id")] public string? PkdsId { get; set; }
        [FromForm(Name = "avatar")] public IFormFile? Avatar { get; set; }

        public UserSaveRequest ToRequest() => Fill(new UserSaveRequest());

        protected T Fill<T>(T target) where T : UserSaveRequest
        {
            target.Name = Name;
            target.Surname = Surname;
            target.Email = Email;
            target.Phone = Phone;
            target.PhoneCountryId = PhoneCountryId;
            target.Password = Password;
            target.WorkingTracking = WorkingTracking;
            target.PkdsId = PkdsId;

            return target;
        }
    }

    public sealed class UserUpdateForm : UserForm
    {
        [FromForm(Name = "id")] public long? Id { get; set; }

        /// <summary>1 ise mevcut avatar silinir.</summary>
        [FromForm(Name = "avatar_remove")] public int AvatarRemove { get; set; }

        public UserUpdateRequest ToUpdateRequest()
        {
            var request = Fill(new UserUpdateRequest());
            request.Id = Id;

            return request;
        }
    }
}
