using System;
using System.Collections.Generic;

namespace OLS.DataAccess.Entities;

public partial class LoadTransferDeliveryMethod
{
    public long Id { get; set; }

    public string? SiberId { get; set; }

    public string? Edikod { get; set; }

    public string? Name { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
