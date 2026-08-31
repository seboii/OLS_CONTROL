using System.Linq.Expressions;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using OLS.Business.Common;
using OLS.Business.Services.Accounts;
using OLS.DataAccess.Context;
using OLS.DataAccess.Entities;

namespace OLS.Business.Services.Users;

/// <summary>
/// Kullanıcı yönetimi.
/// olsold: <c>Front\FrontUser\FrontUserController</c>
///
/// Yeni kullanıcı açılırken <b>tüm yetki sayfaları için sıfır yetkili satır</b>
/// oluşturulur (olsold davranışı) — böylece rol ekranı boş gelmez.
/// </summary>
public interface IUserService
{
    Task<object> ListAsync(UserListQuery query, CancellationToken cancellationToken = default);
    Task<UserDto?> SingleAsync(long id, CancellationToken cancellationToken = default);

    Task<UserDto> CreateAsync(
        UserSaveRequest request, string? avatarPath, CancellationToken cancellationToken = default);

    Task<UserDto?> UpdateAsync(
        UserUpdateRequest request, string? avatarPath, bool removeAvatar,
        CancellationToken cancellationToken = default);

    /// <summary>Silinen kullanıcıların avatar dosyalarını temizleyebilmek için yolları döner.</summary>
    Task<IReadOnlyList<string>> DeleteAsync(
        IReadOnlyList<long> ids, CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(string email, long? exceptId, CancellationToken cancellationToken = default);

    /// <summary>
    /// olsold: <c>UserSave</c>/<c>UserUpdate</c>'in <c>unique:users,phone</c> kuralı.
    /// Kaynak bu kontrolü HAM (normalize edilmemiş) girdiyle yapar; biz normalize edilmiş
    /// (yalnızca rakam) değerle karşılaştırıyoruz — DB'de zaten normalize hâliyle saklanıyor,
    /// bu yüzden gerçek yinelenenleri kaynaktan daha güvenilir yakalar.
    /// </summary>
    Task<bool> PhoneExistsAsync(string phone, long? exceptId, CancellationToken cancellationToken = default);
}

public sealed record UserListQuery(
    string? Search, bool? WorkingTracking, int? PerPage, int Page, string Path,
    bool? Status = null, Guid? PhoneCountryId = null);

public sealed class UserDto
{
    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("name")] public string? Name { get; init; }
    [JsonPropertyName("surname")] public string? Surname { get; init; }
    [JsonPropertyName("email")] public string? Email { get; init; }
    [JsonPropertyName("phone")] public string? Phone { get; init; }
    [JsonPropertyName("avatar")] public string? Avatar { get; init; }
    [JsonPropertyName("working_tracking")] public bool WorkingTracking { get; init; }
    [JsonPropertyName("pkds_id")] public string? PkdsId { get; init; }
    [JsonPropertyName("status")] public bool Status { get; init; }
    /// <summary>Uygulanmış yetki şablonu — Kullanıcılar ekranındaki rol seçici okur.</summary>
    [JsonPropertyName("role_id")] public long? RoleId { get; init; }
    /// <summary>Siber'deki departman adı; rolün neden bu olduğunu ekranda açıklar.</summary>
    [JsonPropertyName("siber_department_name")] public string? SiberDepartmentName { get; init; }
    /// <summary>Siber'de hesap engelli mi (işten ayrılmış).</summary>
    [JsonPropertyName("siber_blocked")] public bool? SiberBlocked { get; init; }
    /// <summary>Görme kapsamı: hangi Siber şirketinin kayıtlarını görür (bkz. CompanyScope).</summary>
    [JsonPropertyName("siber_company_id")] public string? SiberCompanyId { get; init; }

    /// <summary>İlişki adı <c>phoneCountryId</c> → snake_case: <c>phone_country_id</c>.</summary>
    [JsonPropertyName("phone_country_id")] public CountryDto? PhoneCountryId { get; init; }
}

public class UserSaveRequest
{
    public string? Name { get; set; }
    public string? Surname { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public Guid? PhoneCountryId { get; set; }
    public string? Password { get; set; }
    public bool WorkingTracking { get; set; }
    public string? PkdsId { get; set; }
}

public sealed class UserUpdateRequest : UserSaveRequest
{
    public long? Id { get; set; }
}

public sealed class UserService : IUserService
{
    private readonly OlsDbContext _db;
    private readonly IClock _clock;

    public UserService(OlsDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<object> ListAsync(
        UserListQuery query, CancellationToken cancellationToken = default)
    {
        var users = _db.Users.AsNoTracking().Where(u => u.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            // Türkçe noktasız I/ı normalizasyonu için bkz. QueryableExtensions.NormalizeTurkish.
            var pattern = $"%{QueryableExtensions.NormalizeTurkish(query.Search)}%";
            users = users.Where(u =>
                EF.Functions.Like(u.Name.Replace("İ", "i").Replace("I", "i").Replace("ı", "i").ToLower(), pattern) ||
                EF.Functions.Like(u.Surname.Replace("İ", "i").Replace("I", "i").Replace("ı", "i").ToLower(), pattern) ||
                (u.Phone != null && EF.Functions.Like(u.Phone.Replace("İ", "i").Replace("I", "i").Replace("ı", "i").ToLower(), pattern)) ||
                EF.Functions.Like(u.Email.Replace("İ", "i").Replace("I", "i").Replace("ı", "i").ToLower(), pattern) ||
                _db.Countries.Any(c => c.Id == u.PhoneCountryId &&
                                       c.Name != null && EF.Functions.Like(c.Name.Replace("İ", "i").Replace("I", "i").Replace("ı", "i").ToLower(), pattern)));
        }

        // olsold: yalnızca "true ise filtrele" — false gönderilirse süzgeç yok.
        if (query.WorkingTracking == true)
            users = users.Where(u => u.WorkingTracking);

        if (query.Status is { } status)
            users = users.Where(u => u.Status == status);

        if (query.PhoneCountryId is { } phoneCountryId)
            users = users.Where(u => u.PhoneCountryId == phoneCountryId);

        var projected = users.OrderBy(u => u.Name).Select(Project);

        return await projected.ToPagedOrListAsync(
            query.PerPage, query.Page, query.Path, cancellationToken);
    }

    public async Task<UserDto?> SingleAsync(long id, CancellationToken cancellationToken = default) =>
        await _db.Users.AsNoTracking()
            .Where(u => u.Id == id && u.DeletedAt == null)
            .Select(Project)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<UserDto> CreateAsync(
        UserSaveRequest request, string? avatarPath, CancellationToken cancellationToken = default)
    {
        var now = _clock.Now;

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

        var user = new User
        {
            Name = request.Name ?? string.Empty,
            Surname = request.Surname ?? string.Empty,
            Email = request.Email ?? string.Empty,
            Phone = NormalizePhone(request.Phone),
            PhoneCountryId = request.PhoneCountryId,
            Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Avatar = avatarPath,
            Status = true,
            WorkingTracking = request.WorkingTracking,
            PkdsId = request.PkdsId,
            CreatedAt = now,
            UpdatedAt = now,
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        // Her yetki sayfası için sıfır yetkili satır.
        var pageIds = await _db.UserPermissionPages.AsNoTracking()
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        foreach (var pageId in pageIds)
        {
            _db.UserPermissions.Add(new UserPermission
            {
                UserPermissionPageId = pageId,
                UserId = user.Id,
                Read = 0, Create = 0, Update = 0, Delete = 0,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return (await SingleAsync(user.Id, cancellationToken))!;
    }

    public async Task<UserDto?> UpdateAsync(
        UserUpdateRequest request, string? avatarPath, bool removeAvatar,
        CancellationToken cancellationToken = default)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == request.Id && u.DeletedAt == null, cancellationToken);

        if (user is null)
            return null;

        user.Name = request.Name ?? user.Name;
        user.Surname = request.Surname ?? user.Surname;
        user.Email = request.Email ?? user.Email;
        user.Phone = NormalizePhone(request.Phone);
        user.PhoneCountryId = request.PhoneCountryId;
        user.WorkingTracking = request.WorkingTracking;
        user.PkdsId = request.PkdsId;
        user.UpdatedAt = _clock.Now;

        // avatar_remove=1 → temizle; yeni dosya varsa değiştir; yoksa dokunma.
        if (removeAvatar)
            user.Avatar = null;
        else if (avatarPath is not null)
            user.Avatar = avatarPath;

        // Şifre yalnızca doluysa değişir (olsold kuralı).
        if (!string.IsNullOrWhiteSpace(request.Password))
            user.Password = BCrypt.Net.BCrypt.HashPassword(request.Password);

        await _db.SaveChangesAsync(cancellationToken);

        return await SingleAsync(user.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<string>> DeleteAsync(
        IReadOnlyList<long> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
            return [];

        var users = await _db.Users
            .Where(u => ids.Contains(u.Id) && u.DeletedAt == null)
            .ToListAsync(cancellationToken);

        if (users.Count == 0)
            return [];

        var avatars = users.Where(u => u.Avatar is not null).Select(u => u.Avatar!).ToList();
        var now = _clock.Now;

        // users tablosunda soft delete var (deleted_at) — kaynak da öyle siliyor.
        foreach (var user in users)
        {
            user.DeletedAt = now;
            user.UpdatedAt = now;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return avatars;
    }

    public async Task<bool> EmailExistsAsync(
        string email, long? exceptId, CancellationToken cancellationToken = default) =>
        await _db.Users.AsNoTracking()
            .AnyAsync(u => u.Email == email && u.DeletedAt == null &&
                           (exceptId == null || u.Id != exceptId), cancellationToken);

    public async Task<bool> PhoneExistsAsync(
        string phone, long? exceptId, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizePhone(phone);

        return normalized is not null && await _db.Users.AsNoTracking()
            .AnyAsync(u => u.Phone == normalized && u.DeletedAt == null &&
                           (exceptId == null || u.Id != exceptId), cancellationToken);
    }

    /// <summary>
    /// olsold <c>GeneralHelper::phoneNumber()</c>: rakam dışı her şeyi atar.
    /// </summary>
    private static string? NormalizePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return null;

        var digits = new string(phone.Where(char.IsDigit).ToArray());

        return digits.Length == 0 ? null : digits;
    }

    /// <summary>
    /// <c>phoneCountryId</c> ilişkisi tam ülke nesnesi döner (telefon kodu ve
    /// bayrak arayüzde kullanılıyor); DbContext gerektirdiği için örnek alanı.
    /// </summary>
    private Expression<Func<User, UserDto>> Project => u => new UserDto
    {
        Id = u.Id,
        Name = u.Name,
        Surname = u.Surname,
        Email = u.Email,
        Phone = u.Phone,
        Avatar = u.Avatar,
        WorkingTracking = u.WorkingTracking,
        PkdsId = u.PkdsId,
        Status = u.Status,
        RoleId = u.RoleId,
        SiberDepartmentName = u.SiberDepartmentName,
        SiberBlocked = u.SiberBlocked,
        SiberCompanyId = u.SiberCompanyId,
        PhoneCountryId = _db.Countries
            .Where(c => c.Id == u.PhoneCountryId)
            .Select(c => new CountryDto
            {
                Id = c.Id, Name = c.Name, CountryCode = c.CountryCode,
                Flag = c.Flag, PhoneCode = c.PhoneCode, Slug = c.Slug,
            })
            .FirstOrDefault(),
    };
}
