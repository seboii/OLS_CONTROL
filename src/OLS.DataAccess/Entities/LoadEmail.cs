using System;
using System.Collections.Generic;

namespace OLS.DataAccess.Entities;

public partial class LoadEmail
{
    public long Id { get; set; }

    public int? LoadId { get; set; }

    public string? Key { get; set; }

    public string? Email { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
