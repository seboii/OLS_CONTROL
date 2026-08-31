using Microsoft.EntityFrameworkCore;
using OLS.Business.Common;
using OLS.DataAccess.Context;
using OLS.DataAccess.Entities;

namespace OLS.Business.Services.Roles;

public interface IRoleService
{
    /// <summary>Katalogdaki rolleri veritabanına yazar/günceller. İdempotent.</summary>
    Task SyncCatalogAsync(CancellationToken cancellationToken = default);

    /// <summary>Rolün yetki şablonunu kullanıcıya uygular (user_permissions'a yazar).</summary>
    Task<bool> AssignAsync(long userId, long roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Siber departmanına göre tüm kullanıcılara rol atar; Siber'de engelli
    /// olanlara rol vermez ve hesaplarını pasife alır.
    /// </summary>
    Task<RoleAssignmentSummary> ApplyFromSiberAsync(CancellationToken cancellationToken = default);

    /// <summary>Kullanıcının şirket görme kapsamını ayarlar (bkz. CompanyScope).</summary>
    Task<bool> SetCompanyScopeAsync(
        long userId, string? companyId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RoleDto>> ListAsync(CancellationToken cancellationToken = default);
}

public sealed record RoleDto(long Id, string Name, string Slug, string? Description, int UserCount);

public sealed record RoleAssignmentSummary(
    int Assigned, int Deactivated, int Skipped, IReadOnlyDictionary<string, int> PerRole);

public sealed class RoleService : IRoleService
{
    private readonly OlsDbContext _db;
    private readonly IClock _clock;

    public RoleService(OlsDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task SyncCatalogAsync(CancellationToken cancellationToken = default)
    {
        var pageIdBySlug = await _db.UserPermissionPages.AsNoTracking()
            .ToDictionaryAsync(p => p.PermissionPageSlug, p => p.Id,
                StringComparer.OrdinalIgnoreCase, cancellationToken);

        foreach (var definition in RoleCatalog.All)
        {
            var role = await _db.Roles
                .Include(r => r.RolePermissions)
                .FirstOrDefaultAsync(r => r.Slug == definition.Slug, cancellationToken);

            if (role is null)
            {
                role = new Role { Slug = definition.Slug, CreatedAt = _clock.Now };
                _db.Roles.Add(role);
            }

            role.Name = definition.Name;
            role.Description = definition.Description;
            role.IsDefault = definition.IsDefault;
            role.UpdatedAt = _clock.Now;
            await _db.SaveChangesAsync(cancellationToken);

            // Şablon katalogla BİREBİR olmalı: katalogdan çıkarılan bir sayfa
            // rolde asılı kalmasın diye mevcut satırlar önce temizlenir.
            _db.RolePermissions.RemoveRange(role.RolePermissions);
            await _db.SaveChangesAsync(cancellationToken);

            foreach (var page in definition.Pages)
            {
                if (!pageIdBySlug.TryGetValue(page.Slug, out var pageId))
                    continue;

                _db.RolePermissions.Add(new RolePermission
                {
                    RoleId = role.Id,
                    UserPermissionPageId = pageId,
                    Read = page.Read ? 1 : 0,
                    Create = page.Create ? 1 : 0,
                    Update = page.Update ? 1 : 0,
                    Delete = page.Delete ? 1 : 0,
                });
            }

            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> AssignAsync(
        long userId, long roleId, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null || !await _db.Roles.AnyAsync(r => r.Id == roleId, cancellationToken))
            return false;

        var byPage = await _db.RolePermissions.AsNoTracking()
            .Where(rp => rp.RoleId == roleId)
            .ToDictionaryAsync(rp => rp.UserPermissionPageId, cancellationToken);

        var current = await _db.UserPermissions
            .Where(p => p.UserId == userId)
            .ToListAsync(cancellationToken);

        var pageIds = await _db.UserPermissionPages.AsNoTracking()
            .Select(p => p.Id).ToListAsync(cancellationToken);

        // HER sayfa için satır yazılır. Şablonda olmayan sayfa sıfırlanır —
        // aksi hâlde önceki rolden kalan yetki sessizce sürerdi.
        foreach (var pageId in pageIds)
        {
            var row = current.FirstOrDefault(p => p.UserPermissionPageId == pageId);
            if (row is null)
            {
                row = new UserPermission { UserId = userId, UserPermissionPageId = pageId };
                _db.UserPermissions.Add(row);
            }

            byPage.TryGetValue(pageId, out var template);
            // user_permissions sütunları short; role_permissions int tutuyor.
            row.Read = (short)(template?.Read ?? 0);
            row.Create = (short)(template?.Create ?? 0);
            row.Update = (short)(template?.Update ?? 0);
            row.Delete = (short)(template?.Delete ?? 0);
        }

        user.RoleId = roleId;
        user.UpdatedAt = _clock.Now;

        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<RoleAssignmentSummary> ApplyFromSiberAsync(
        CancellationToken cancellationToken = default)
    {
        await SyncCatalogAsync(cancellationToken);

        var roles = await _db.Roles.AsNoTracking().ToListAsync(cancellationToken);
        var roleBySlug = roles.ToDictionary(r => r.Slug, r => r.Id, StringComparer.OrdinalIgnoreCase);
        var defaultRoleId = roles.FirstOrDefault(r => r.IsDefault)?.Id;

        var users = await _db.Users.ToListAsync(cancellationToken);

        var perRole = new Dictionary<string, int>();
        int assigned = 0, deactivated = 0, skipped = 0;

        foreach (var user in users)
        {
            // Siber karşılığı olmayan hesaplar (kurulum admini, test hesapları)
            // bu akışın dışında: yetkileri elle yönetiliyor.
            if (string.IsNullOrWhiteSpace(user.SiberId))
            {
                skipped++;
                continue;
            }

            if (user.SiberBlocked == true)
            {
                if (user.Status)
                {
                    user.Status = false;
                    user.UpdatedAt = _clock.Now;
                    deactivated++;
                }

                continue;
            }

            var slug = user.SiberDepartmentName is { } name &&
                       RoleCatalog.DepartmentToRoleSlug.TryGetValue(
                           QueryableExtensions.NormalizeTurkish(name), out var mapped)
                ? mapped
                : null;

            var roleId = slug is not null && roleBySlug.TryGetValue(slug, out var mappedId)
                ? mappedId
                : defaultRoleId;

            if (roleId is null)
                continue;

            await AssignAsync(user.Id, roleId.Value, cancellationToken);
            assigned++;

            var roleName = roles.First(r => r.Id == roleId.Value).Name;
            perRole[roleName] = perRole.GetValueOrDefault(roleName) + 1;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return new RoleAssignmentSummary(assigned, deactivated, skipped, perRole);
    }

    public async Task<bool> SetCompanyScopeAsync(
        long userId, string? companyId, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
            return false;

        user.SiberCompanyId = string.IsNullOrWhiteSpace(companyId) ? null : companyId.Trim();
        user.UpdatedAt = _clock.Now;

        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<RoleDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var counts = await _db.Users.AsNoTracking()
            .Where(u => u.RoleId != null)
            .GroupBy(u => u.RoleId!.Value)
            .Select(g => new { RoleId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RoleId, x => x.Count, cancellationToken);

        var roles = await _db.Roles.AsNoTracking().OrderBy(r => r.Name).ToListAsync(cancellationToken);

        return roles
            .Select(r => new RoleDto(r.Id, r.Name, r.Slug, r.Description, counts.GetValueOrDefault(r.Id)))
            .ToList();
    }
}
