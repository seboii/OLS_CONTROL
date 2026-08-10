using System;
using System.Collections.Generic;

namespace OLS.DataAccess.Entities;

public partial class UserPermissionPage
{
    public long Id { get; set; }

    public string PermissionPageName { get; set; } = null!;

    public string PermissionPageSlug { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
}
