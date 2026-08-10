using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using OLS.Business.Common;
using OLS.Business.Services.Authentication;
using OLS.DataAccess.Context;

namespace OLS.Business.Services.Users;

/// <summary>
/// Oturum açan kullanıcının kendi profili.
/// olsold: <c>Front\Profile\ProfileController</c>
///
///   GET  /profile                  kendi kaydı
///   POST /profile/update           ad+soyad+iletişim+avatar (+opsiyonel şifre)
///   POST /profile/general/update   ad+soyad+ülke+avatar
///   POST /profile/contact/update   e-posta+telefon
///   POST /profile/password/update  mevcut şifre doğrulamalı
///   DELETE /profile                kendi hesabını siler
///
/// Arayüz yalnızca <c>GET</c> ve üç parçalı güncellemeyi kullanıyor
/// (<c>data_store.js</c>); <c>/update</c> ve <c>DELETE</c> çağrılmıyor ama
/// rota uyumu için portlandı.
///
/// Avatar dosya adı API katmanında üretilir (<c>IFileStorage</c>); bu servis
/// yalnızca adı saklar.
/// </summary>
public interface IProfileService
{
    Task<ProfileDto?> GetAsync(long userId, CancellationToken cancellationToken = default);

    Task<ProfileDto?> UpdateAsync(
        long userId, ProfileInput input, CancellationToken cancellationToken = default);

    Task<ProfileDto?> UpdateGeneralAsync(
        long userId, ProfileInput input, CancellationToken cancellationToken = default);

    Task<ProfileDto?> UpdateContactAsync(
        long userId, ProfileInput input, CancellationToken cancellationToken = default);

    Task<PasswordChangeResult> UpdatePasswordAsync(
        long userId, string? currentPassword, string? newPassword, string? confirmation,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>Avatar değişiminde silinmesi gereken eski dosya adı.</summary>
    Task<string?> CurrentAvatarAsync(long userId, CancellationToken cancellationToken = default);
}

public sealed class ProfileInput
{
    public string? Name { get; set; }
    public string? Surname { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public Guid? PhoneCountryId { get; set; }
    public Guid? CountryId { get; set; }
    public string? Password { get; set; }

    /// <summary>Yeni yüklenen avatarın adı; yükleme yoksa null.</summary>
    public string? AvatarFileName { get; set; }

    /// <summary>Arayüz <c>avatar_remove=1</c> gönderirse avatar temizlenir.</summary>
    public bool RemoveAvatar { get; set; }
}

public sealed record PasswordChangeResult(bool Success, string? ErrorMessage);

public sealed class ProfileDto
{
    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("name")] public string? Name { get; init; }
    [JsonPropertyName("surname")] public string? Surname { get; init; }
    [JsonPropertyName("email")] public string? Email { get; init; }
    [JsonPropertyName("phone")] public string? Phone { get; init; }
    [JsonPropertyName("avatar")] public string? Avatar { get; init; }
    [JsonPropertyName("phone_country_id")] public ProfileCountryDto? PhoneCountryId { get; init; }
    [JsonPropertyName("country_id")] public ProfileCountryDto? CountryId { get; init; }
}

public sealed class ProfileCountryDto
{
    [JsonPropertyName("id")] public Guid Id { get; init; }
    [JsonPropertyName("name")] public string? Name { get; init; }
    [JsonPropertyName("phone_code")] public string? PhoneCode { get; init; }
    [JsonPropertyName("country_code")] public string? Iso2 { get; init; }
}

public sealed class ProfileService : IProfileService
{
    private readonly OlsDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IClock _clock;

    public ProfileService(OlsDbContext db, IPasswordHasher hasher, IClock clock)
    {
        _db = db;
        _hasher = hasher;
        _clock = clock;
    }

    public async Task<ProfileDto?> GetAsync(
        long userId, CancellationToken cancellationToken = default) =>
        await _db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new ProfileDto
            {
                Id = u.Id,
                Name = u.Name,
                Surname = u.Surname,
                Email = u.Email,
                Phone = u.Phone,
                Avatar = u.Avatar,
                PhoneCountryId = _db.Countries.Where(c => c.Id == u.PhoneCountryId)
                    .Select(c => new ProfileCountryDto
                    {
                        Id = c.Id, Name = c.Name, PhoneCode = c.PhoneCode, Iso2 = c.CountryCode,
                    })
                    .FirstOrDefault(),
                CountryId = _db.Countries.Where(c => c.Id == u.CountryId)
                    .Select(c => new ProfileCountryDto
                    {
                        Id = c.Id, Name = c.Name, PhoneCode = c.PhoneCode, Iso2 = c.CountryCode,
                    })
                    .FirstOrDefault(),
            })
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<string?> CurrentAvatarAsync(
        long userId, CancellationToken cancellationToken = default) =>
        await _db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.Avatar)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<ProfileDto?> UpdateAsync(
        long userId, ProfileInput input, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
            return null;

        user.Name = input.Name ?? user.Name;
        user.Surname = input.Surname ?? user.Surname;
        user.Email = input.Email ?? user.Email;
        user.Phone = NormalizePhone(input.Phone);
        user.PhoneCountryId = input.PhoneCountryId;
        user.Avatar = ResolveAvatar(user.Avatar, input);

        // Kaynak: şifre yalnızca DOLU gönderilirse değişir.
        if (!string.IsNullOrWhiteSpace(input.Password))
            user.Password = _hasher.Hash(input.Password);

        user.UpdatedAt = _clock.Now;
        await _db.SaveChangesAsync(cancellationToken);

        return await GetAsync(userId, cancellationToken);
    }

    public async Task<ProfileDto?> UpdateGeneralAsync(
        long userId, ProfileInput input, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
            return null;

        user.Name = input.Name ?? user.Name;
        user.Surname = input.Surname ?? user.Surname;
        user.CountryId = input.CountryId;
        user.Avatar = ResolveAvatar(user.Avatar, input);
        user.UpdatedAt = _clock.Now;

        await _db.SaveChangesAsync(cancellationToken);

        return await GetAsync(userId, cancellationToken);
    }

    public async Task<ProfileDto?> UpdateContactAsync(
        long userId, ProfileInput input, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
            return null;

        user.Email = input.Email ?? user.Email;
        user.Phone = NormalizePhone(input.Phone);
        user.PhoneCountryId = input.PhoneCountryId;
        user.UpdatedAt = _clock.Now;

        await _db.SaveChangesAsync(cancellationToken);

        return await GetAsync(userId, cancellationToken);
    }

    public async Task<PasswordChangeResult> UpdatePasswordAsync(
        long userId, string? currentPassword, string? newPassword, string? confirmation,
        CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
            return new PasswordChangeResult(false, "Kayıt Bulunamadı");

        if (string.IsNullOrEmpty(user.Password) ||
            !_hasher.Verify(currentPassword ?? string.Empty, user.Password))
            return new PasswordChangeResult(false, "Geçerli şifre yanlış.");

        if (newPassword != confirmation)
            return new PasswordChangeResult(false, "Yeni şifre onayı hatalı.");

        user.Password = _hasher.Hash(newPassword ?? string.Empty);
        user.UpdatedAt = _clock.Now;

        await _db.SaveChangesAsync(cancellationToken);

        return new PasswordChangeResult(true, null);
    }

    public async Task<bool> DeleteAsync(long userId, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
            return false;

        _db.Users.Remove(user);
        await _db.SaveChangesAsync(cancellationToken);

        return true;
    }

    /// <summary>olsold <c>GeneralHelper::phoneNumber()</c>: rakam dışı her şeyi atar.</summary>
    private static string? NormalizePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return null;

        var digits = new string(phone.Where(char.IsDigit).ToArray());

        return digits.Length == 0 ? null : digits;
    }

    /// <summary>
    /// Kaynaktaki üçlü mantık: <c>avatar_remove=1</c> → temizle, yeni dosya
    /// geldiyse onu kullan, ikisi de yoksa mevcut adı koru.
    /// </summary>
    private static string? ResolveAvatar(string? current, ProfileInput input)
    {
        if (input.RemoveAvatar)
            return null;

        return input.AvatarFileName ?? current;
    }
}
