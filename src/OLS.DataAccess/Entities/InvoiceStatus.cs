using System;
using System.Collections.Generic;

namespace OLS.DataAccess.Entities;

public partial class InvoiceStatus
{
    public long Id { get; set; }

    public string EnumValue { get; set; } = null!;

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
