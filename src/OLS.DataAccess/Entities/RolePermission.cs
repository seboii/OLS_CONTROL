namespace OLS.DataAccess.Entities;

/// <summary>Rolün bir yetki sayfası üzerindeki CRUD şablonu.</summary>
public partial class RolePermission
{
    public long Id { get; set; }

    public long RoleId { get; set; }

    public long UserPermissionPageId { get; set; }

    public int Read { get; set; }

    public int Create { get; set; }

    public int Update { get; set; }

    public int Delete { get; set; }

    public virtual Role Role { get; set; } = null!;
}
