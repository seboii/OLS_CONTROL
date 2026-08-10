using System;
using System.Collections.Generic;

namespace OLS.DataAccess.Entities;

public partial class StatusType
{
    public long Id { get; set; }

    public string? Name { get; set; }

    public string? Number { get; set; }

    public string? SiberId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
