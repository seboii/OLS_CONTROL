using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OLS.API.Services;
using OLS.Business.Common;
using OLS.Business.Services.Authorization;
using OLS.Business.Services.Users;

namespace OLS.API.Controllers.Front;

/// <summary>
/// olsold: <c>Front\Profile\ProfileController</c> — oturum açan kullanıcının profili.
///
///   GET    /api/v1/profile
///   POST   /api/v1/profile/update
///   POST   /api/v1/profile/general/update
///   POST   /api/v1/profile/contact/update
///   POST   /api/v1/profile/password/update
///   DELETE /api/v1/profile
///
/// Hepsi <c>FormData</c> alır (avatar dosyası taşıdığı için).
/// Kullanıcı kimliği daima token'dan gelir — gövdeden gelen bir id kabul
/// edilmez; aksi hâlde herkes başkasının profilini değiştirebilirdi.
/// </summary>
[Authorize]
[Route("api/v1/profile")]
public sealed class ProfileController : ApiControllerBase
{
    private readonly IProfileService _profile;
    private readonly ICurrentUser _currentUser;
    private readonly IFileStorage _files;

    public ProfileController(
        IProfileService profile, ICurrentUser currentUser, IFileStorage files)
    {
        _profile = profile;
        _currentUser = currentUser;
        _files = files;
    }

    [HttpGet]
    public async Task<IActionResult> All(CancellationToken cancellationToken)
    {
        if (_currentUser.Id is not { } userId)
            return Unauthorized(ApiResponse.Error(Translator.Get("Yetkisiz Erişim")));

        var profile = await _profile.GetAsync(userId, cancellationToken);

        return profile is null ? NotFoundError() : Ok(profile, "Kayıtlar");
    }

    [HttpPost("update")]
    public async Task<IActionResult> Update(
        [FromForm] ProfileForm form, CancellationToken cancellationToken)
    {
        if (_currentUser.Id is not { } userId)
            return Unauthorized(ApiResponse.Error(Translator.Get("Yetkisiz Erişim")));

        var input = await BuildInputAsync(userId, form, cancellationToken);
        var updated = await _profile.UpdateAsync(userId, input, cancellationToken);

        return updated is null ? NotFoundError() : Ok(updated, "Güncelleme Başarılı");
    }

    [HttpPost("general/update")]
    public async Task<IActionResult> GeneralUpdate(
        [FromForm] ProfileForm form, CancellationToken cancellationToken)
    {
        if (_currentUser.Id is not { } userId)
            return Unauthorized(ApiResponse.Error(Translator.Get("Yetkisiz Erişim")));

        var input = await BuildInputAsync(userId, form, cancellationToken);
        var updated = await _profile.UpdateGeneralAsync(userId, input, cancellationToken);

        return updated is null ? NotFoundError() : Ok(updated, "Güncelleme Başarılı");
    }

    [HttpPost("contact/update")]
    public async Task<IActionResult> ContactUpdate(
        [FromForm] ProfileForm form, CancellationToken cancellationToken)
    {
        if (_currentUser.Id is not { } userId)
            return Unauthorized(ApiResponse.Error(Translator.Get("Yetkisiz Erişim")));

        var updated = await _profile.UpdateContactAsync(
            userId,
            new ProfileInput
            {
                Email = form.Email,
                Phone = form.Phone,
                PhoneCountryId = form.PhoneCountryId,
            },
            cancellationToken);

        return updated is null ? NotFoundError() : Ok(updated, "Güncelleme Başarılı");
    }

    /// <summary>
    /// Mevcut şifre yanlışsa veya onay eşleşmezse kaynak <c>400 {"message": …}</c>
    /// döndürüyor (doğrulama zarfı değil) — korundu.
    /// </summary>
    [HttpPost("password/update")]
    public async Task<IActionResult> PasswordUpdate(
        [FromForm] PasswordForm form, CancellationToken cancellationToken)
    {
        if (_currentUser.Id is not { } userId)
            return Unauthorized(ApiResponse.Error(Translator.Get("Yetkisiz Erişim")));

        var result = await _profile.UpdatePasswordAsync(
            userId, form.CurrentPassword, form.NewPassword, form.NewPasswordConfirmation,
            cancellationToken);

        if (!result.Success)
            return BadRequest(ApiResponse.Message(result.ErrorMessage!));

        var profile = await _profile.GetAsync(userId, cancellationToken);

        return Ok(profile, "Şifre güncellemesi başarılı.");
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(CancellationToken cancellationToken)
    {
        if (_currentUser.Id is not { } userId)
            return Unauthorized(ApiResponse.Error(Translator.Get("Yetkisiz Erişim")));

        var deleted = await _profile.DeleteAsync(userId, cancellationToken);

        return deleted
            ? base.Ok(ApiResponse.Message("Kayıt Başarıyla Silindi"))
            : NotFoundError();
    }

    /// <summary>
    /// Avatar mantığı kaynaktaki gibi üç yollu: <c>avatar_remove=1</c> ise eski
    /// dosya silinir ve alan boşaltılır; yeni dosya geldiyse eskisi silinip
    /// yenisi yazılır; ikisi de yoksa mevcut ad korunur.
    /// </summary>
    private async Task<ProfileInput> BuildInputAsync(
        long userId, ProfileForm form, CancellationToken cancellationToken)
    {
        var remove = form.AvatarRemove == "1";
        var current = await _profile.CurrentAvatarAsync(userId, cancellationToken);

        string? stored = null;

        if (remove)
        {
            _files.Delete(current);
        }
        else if (form.Avatar is not null)
        {
            stored = await _files.SaveAvatarAsync(form.Avatar, cancellationToken);

            if (stored is not null)
                _files.Delete(current);
        }

        return new ProfileInput
        {
            Name = form.Name,
            Surname = form.Surname,
            Email = form.Email,
            Phone = form.Phone,
            PhoneCountryId = form.PhoneCountryId,
            CountryId = form.CountryId,
            Password = form.Password,
            AvatarFileName = stored,
            RemoveAvatar = remove,
        };
    }

    public sealed class ProfileForm
    {
        [FromForm(Name = "name")] public string? Name { get; set; }
        [FromForm(Name = "surname")] public string? Surname { get; set; }
        [FromForm(Name = "email")] public string? Email { get; set; }
        [FromForm(Name = "phone")] public string? Phone { get; set; }
        [FromForm(Name = "phone_country_id")] public Guid? PhoneCountryId { get; set; }
        [FromForm(Name = "country_id")] public Guid? CountryId { get; set; }
        [FromForm(Name = "password")] public string? Password { get; set; }
        [FromForm(Name = "avatar")] public IFormFile? Avatar { get; set; }
        [FromForm(Name = "avatar_remove")] public string? AvatarRemove { get; set; }
    }

    public sealed class PasswordForm
    {
        [FromForm(Name = "current_password")] public string? CurrentPassword { get; set; }
        [FromForm(Name = "new_password")] public string? NewPassword { get; set; }
        [FromForm(Name = "new_password_confirmation")] public string? NewPasswordConfirmation { get; set; }
    }
}
