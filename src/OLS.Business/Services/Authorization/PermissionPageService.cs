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
/// Sayfa AÇMAK geliştirici işidir: bir slug ancak kod onu kontrol ediyorsa
/// anlam taşır, kimsenin bakmadığı sayfa hiçbir şey yapmaz. Silme ise
/// yönetilebilir olmalı — elle açılmış artık sayfalar aksi hâlde ekranda
/// sonsuza kadar kalıyordu (canlıda "test_sayfa_canli" tam olarak öyle kaldı).
/// </summary>
public interface IPermissionPageService
{
    Task<PermissionPageResult> CreateAsync(
        string pageName, string pageSlug, CancellationToken cancellationToken = default);

    /// <summary>
    /// Yetki sayfasını ve ona bağlı tüm kullanıcı yetki satırlarını siler.
    ///
    /// PROGRAMIN KULLANDIĞI sayfa SİLİNEMEZ (bkz. <see cref="PermissionPages"/>).
    /// Sebep ters yönde ve sessiz: <c>PermissionService</c> bulunamayan bir slug
    /// için <b>true</b> döner, yani sayfayı silmek modülü kilitlemez — HERKESE
    /// AÇAR. Silme yalnızca elle açılmış, kodun hiç bakmadığı sayfalar içindir.
    /// </summary>
    Task<PermissionPageResult> DeleteAsync(
        string pageSlug, CancellationToken cancellationToken = default);
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
    public async Task<PermissionPageResult> DeleteAsync(
        string pageSlug, CancellationToken cancellationToken = default)
    {
        // Kimlik değil SLUG ile: yetki ekranını besleyen uç
        // (UserPermissionService.GetAsync) satırın user_permissions kimliğini
        // veriyor, sayfa kimliğini HİÇ döndürmüyor. Slug hem o veride var hem
        // de ortamdan ortama değişmiyor.
        var page = await _db.UserPermissionPages
            .FirstOrDefaultAsync(p => p.PermissionPageSlug == pageSlug, cancellationToken);

        if (page is null)
            return new PermissionPageResult(false, "Yetki sayfası bulunamadı.");

        // Programın kullandığı sayfa silinirse o modül yetkisiz kalmaz,
        // YETKİSİZ AÇILIR — bkz. PermissionPages.
        if (PermissionPages.IsUsedByProgram(page.PermissionPageSlug))
            return new PermissionPageResult(false,
                $"\"{page.PermissionPageName}\" programın kullandığı bir yetki sayfası; " +
                "silinemez. Silinseydi bu modül yetki kontrolü olmadan herkese açılırdı.");

        var rows = await _db.UserPermissions
            .Where(p => p.UserPermissionPageId == page.Id)
            .ToListAsync(cancellationToken);

        _db.UserPermissions.RemoveRange(rows);
        _db.UserPermissionPages.Remove(page);
        await _db.SaveChangesAsync(cancellationToken);

        return new PermissionPageResult(true,
            $"\"{page.PermissionPageName}\" silindi ({rows.Count} kullanıcı yetki satırı).");
    }
}
