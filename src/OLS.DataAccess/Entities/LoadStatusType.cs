using System;
using System.Collections.Generic;

namespace OLS.DataAccess.Entities;

public partial class LoadStatusType
{
    public long Id { get; set; }

    public string? Name { get; set; }

    public int? LoadStatusId { get; set; }

    public int? OrderNo { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
