using System;
using System.Collections.Generic;

namespace OLS.DataAccess.Entities;

public partial class FinancialItem
{
    public long Id { get; set; }

    public string Name { get; set; } = null!;

    public int? Type { get; set; }

    public string? SiberId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
