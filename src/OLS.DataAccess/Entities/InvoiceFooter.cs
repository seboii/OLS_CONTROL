using System;
using System.Collections.Generic;

namespace OLS.DataAccess.Entities;

public partial class InvoiceFooter
{
    public long Id { get; set; }

    public long InvoiceId { get; set; }

    public string Value { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Invoice Invoice { get; set; } = null!;
}
