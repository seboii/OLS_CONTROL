using Microsoft.EntityFrameworkCore;
using OLS.Business.Services.Authorization;
using OLS.DataAccess.Context;

namespace OLS.Business.Services.Roles;

/// <summary>
/// olsold: <c>App\Http\Controllers\Front\Role\UserPermissionController</c>
///
/// Frontend her sayfa geçişinde bu ucu çağırıp kullanıcının yetki matrisini
/// yüklüyor (router/index.js -> DataStore.GET_USER_ROLE).
/// </summary>
public interface IUserPermissionService
{
    Task<UserRoleResult?> GetAsync(long userId, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(
        string crud,
        short isData,
        long? permissionRowId,
        long? userId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Frontend <c>res.data.stats.permission_data</c> ve <c>stats.user_name</c>
/// okuyor; alan adları bu yüzden sabit.
/// </summary>
public sealed record UserRoleResult(string UserName, IReadOnlyList<PermissionRow> PermissionData);

public sealed record PermissionRow(
    long Id,
    short Read,
    short Create,
    short Update,
    short Delete,
    string PermissionPageName,
    string PermissionPageSlug);

public sealed class UserPermissionService : IUserPermissionService
{
    private static readonly string[] AllowedCrud = ["read", "create", "update", "delete"];

    private readonly OlsDbContext _db;

    public UserPermissionService(OlsDbContext db) => _db = db;

    public async Task<UserRoleResult?> GetAsync(long userId, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users
            .Where(u => u.Id == userId)
            .Select(u => new { u.Name, u.Surname })
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
            return null;

        // olsold: CONCAT(name,' ',surname) as user_name
        var userName = $"{user.Name} {user.Surname}";

        var rows = await _db.UserPermissions
            .Where(p => p.UserId == userId)
            .Join(_db.UserPermissionPages,
                p => p.UserPermissionPageId,
                page => page.Id,
                (p, page) => new PermissionRow(
                    p.Id,
                    p.Read,
                    p.Create,
                    p.Update,
                    p.Delete,
                    page.PermissionPageName,
                    page.PermissionPageSlug))
            .ToListAsync(cancellationToken);

        return new UserRoleResult(userName, rows);
    }

    /// <summary>
    /// Tek bir yetki satırını ya da kullanıcının tüm satırlarını günceller.
    /// olsold davranışı: permission_page_id verilmişse o satır, verilmemişse
    /// user_id'ye ait TÜM satırlar aynı değere çekilir.
    /// </summary>
    public async Task<bool> UpdateAsync(
        string crud,
        short isData,
        long? permissionRowId,
        long? userId,
        CancellationToken cancellationToken = default)
    {
        if (!AllowedCrud.Contains(crud, StringComparer.OrdinalIgnoreCase))
            return false;

        if (permissionRowId is { } rowId)
        {
            var row = await _db.UserPermissions
                .FirstOrDefaultAsync(p => p.Id == rowId, cancellationToken);

            if (row is null)
                return false;

            Apply(row, crud, isData);
        }
        else if (userId is { } uid)
        {
            var rows = await _db.UserPermissions
                .Where(p => p.UserId == uid)
                .ToListAsync(cancellationToken);

            if (rows.Count == 0)
                return false;

            foreach (var row in rows)
                Apply(row, crud, isData);
        }
        else
        {
            return false;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static void Apply(DataAccess.Entities.UserPermission row, string crud, short value)
    {
        switch (crud.ToLowerInvariant())
        {
            case "read": row.Read = value; break;
            case "create": row.Create = value; break;
            case "update": row.Update = value; break;
            case "delete": row.Delete = value; break;
        }
    }
}
