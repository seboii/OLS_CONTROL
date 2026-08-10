using Microsoft.EntityFrameworkCore;
using OLS.Business.Common;
using OLS.DataAccess.Context;
using OLS.DataAccess.Entities;

namespace OLS.Business.Services.Authorization;

/// <summary>
/// Yeni yetki sayfası tanımlama — <c>POST /permission</c>.
/// olsold: <c>Front\Permission\PermissionController::save</c>
///
/// Yeni bir <c>user_permission_pages</c> satırı açar ve <b>tüm kullanıcılara</b>
/// bu sayfa için dört hakkı da (read/create/update/delete) 1 olarak verir.
/// Yani yeni bir modül eklendiğinde kimse kilitlenmesin diye açık başlar;
/// kısıtlama sonradan kullanıcı yetki ekranından yapılır.
///
/// Bu uç geliştirici aracıdır — arayüzde çağıran bir ekran yok.
/// </summary>
public interface IPermissionPageService
{
    Task<PermissionPageResult> CreateAsync(
        string pageName, string pageSlug, CancellationToken cancellationToken = default);
}

public sealed record PermissionPageResult(bool Success, string Message);

public sealed class PermissionPageService : IPermissionPageService
{
    private readonly OlsDbContext _db;
    private readonly IClock _clock;

    public PermissionPageService(OlsDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<PermissionPageResult> CreateAsync(
        string pageName, string pageSlug, CancellationToken cancellationToken = default)
    {
        var exists = await _db.UserPermissionPages
            .AnyAsync(p => p.PermissionPageSlug == pageSlug, cancellationToken);

        if (exists)
            return new PermissionPageResult(false, "Bu yetki sayfası zaten mevcut.");

        var now = _clock.Now;

        var page = new UserPermissionPage
        {
            PermissionPageName = pageName,
            PermissionPageSlug = pageSlug,
            CreatedAt = now,
            UpdatedAt = now,
        };

        _db.UserPermissionPages.Add(page);
        await _db.SaveChangesAsync(cancellationToken);

        var userIds = await _db.Users
            .Where(u => u.DeletedAt == null)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        foreach (var userId in userIds)
        {
            _db.UserPermissions.Add(new UserPermission
            {
                UserPermissionPageId = page.Id,
                UserId = userId,
                Read = 1,
                Update = 1,
                Create = 1,
                Delete = 1,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new PermissionPageResult(true, "Yetki sayfası başarıyla oluşturuldu.");
    }
}
