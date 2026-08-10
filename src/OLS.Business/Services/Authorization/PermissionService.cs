using Microsoft.EntityFrameworkCore;
using OLS.DataAccess.Context;

namespace OLS.Business.Services.Authorization;

/// <summary>
/// olsold'daki <c>App\Classes\Helpers\RoleHelper</c> karşılığı.
/// Sorgu mantığı aynı: önce slug'dan sayfa id'si bulunur, sonra
/// (user_id, user_permission_page_id) çifti için ilgili CRUD bayrağı aranır.
/// </summary>
public sealed class PermissionService : IPermissionService
{
    private readonly OlsDbContext _db;

    public PermissionService(OlsDbContext db) => _db = db;

    public async Task<bool> HasPermissionAsync(
        long userId,
        string pageSlug,
        PermissionAction action,
        CancellationToken cancellationToken = default)
    {
        var pageId = await _db.UserPermissionPages
            .Where(p => p.PermissionPageSlug == pageSlug)
            .Select(p => (long?)p.Id)
            .FirstOrDefaultAsync(cancellationToken);

        // Slug tanımlı değilse serbest: RoleHelper de bu durumda true dönüyordu.
        if (pageId is null)
            return true;

        var query = _db.UserPermissions
            .Where(p => p.UserId == userId && p.UserPermissionPageId == pageId);

        // Bayraklar veritabanında smallint; Laravel de where('read', 1) ile karşılaştırıyordu.
        query = action switch
        {
            PermissionAction.Read => query.Where(p => p.Read == 1),
            PermissionAction.Create => query.Where(p => p.Create == 1),
            PermissionAction.Update => query.Where(p => p.Update == 1),
            PermissionAction.Delete => query.Where(p => p.Delete == 1),
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, null),
        };

        return await query.AnyAsync(cancellationToken);
    }
}
