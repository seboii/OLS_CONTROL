using System;
using System.Collections.Generic;

namespace OLS.DataAccess.Entities;

public partial class UserPermission
{
    public long Id { get; set; }

    public long UserPermissionPageId { get; set; }

    public long UserId { get; set; }

    public short Read { get; set; }

    public short Update { get; set; }

    public short Create { get; set; }

    public short Delete { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User User { get; set; } = null!;

    public virtual UserPermissionPage UserPermissionPage { get; set; } = null!;
}
