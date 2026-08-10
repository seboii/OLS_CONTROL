using System;
using System.Collections.Generic;

namespace OLS.DataAccess.Entities;

public partial class ItemType
{
    public long Id { get; set; }

    public string Name { get; set; } = null!;

    public string? SiberId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
