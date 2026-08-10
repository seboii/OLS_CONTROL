using System;
using System.Collections.Generic;

namespace OLS.DataAccess.Entities;

public partial class LoadMovement
{
    public long Id { get; set; }

    public long LoadId { get; set; }

    public int? MovementTypeId { get; set; }

    public string? Note { get; set; }

    public string? SiberId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Load Load { get; set; } = null!;
}
