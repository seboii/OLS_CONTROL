namespace OLS.Business.Services.Authorization;

/// <summary>
/// olsold'daki CRUD bayrakları: user_permissions tablosunun
/// read / create / update / delete sütunları.
/// </summary>
public enum PermissionAction
{
    Read,
    Create,
    Update,
    Delete,
}

public interface IPermissionService
{
    /// <summary>
    /// Kullanıcının ilgili sayfa slug'ı üzerinde istenen CRUD yetkisi var mı?
    ///
    /// olsold davranışını koruyoruz: <c>user_permission_pages</c> içinde slug
    /// bulunamazsa <c>true</c> döner (RoleHelper de öyle yapıyordu — tanımsız
    /// sayfa serbest sayılır). Slug tanımlıysa yetki kaydı aranır.
    /// </summary>
    Task<bool> HasPermissionAsync(
        long userId,
        string pageSlug,
        PermissionAction action,
        CancellationToken cancellationToken = default);
}

/// <summary>İstekteki kimliği doğrulanmış kullanıcı.</summary>
public interface ICurrentUser
{
    long? Id { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
}
