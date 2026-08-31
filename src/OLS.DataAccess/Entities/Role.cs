namespace OLS.DataAccess.Entities;

/// <summary>
/// Yetki şablonu. Yetkinin KENDİSİ değil, bir şablondur: role atanmış sayfa
/// izinleri kullanıcıya uygulandığında <see cref="UserPermission"/> satırlarına
/// yazılır ve yetki kontrolü (RequiresPermissionAttribute → IPermissionService)
/// eskisi gibi o satırları okur. Böylece rol kavramı eklenirken yetki
/// uygulamasının tek doğruluk noktası değişmemiş olur.
///
/// Roller Siber'deki departmanlardan türetilir (bkz. DbSeeder.SeedRolesAsync):
/// kullanıcının sky_kullanici.departmanid değeri hangi departmanı gösteriyorsa
/// o departmanın rolü atanır.
/// </summary>
public partial class Role
{
    public long Id { get; set; }

    public string Name { get; set; } = null!;

    /// <summary>Kod adı — departman eşlemesi ve seed idempotansı bunun üzerinden.</summary>
    public string Slug { get; set; } = null!;

    public string? Description { get; set; }

    /// <summary>
    /// Departmanı olmayan kullanıcılara uygulanan varsayılan rol. Tam olarak bir
    /// rolde true olması beklenir.
    /// </summary>
    public bool IsDefault { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
