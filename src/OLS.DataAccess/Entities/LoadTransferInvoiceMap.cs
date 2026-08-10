using System;
using System.Collections.Generic;

namespace OLS.DataAccess.Entities;

public partial class LoadTransferInvoiceMap
{
    public long Id { get; set; }

    public long LoadTransferId { get; set; }

    public long InvoiceItemId { get; set; }

    public long InvoiceId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Invoice Invoice { get; set; } = null!;

    public virtual LoadTransferInvoiceItem InvoiceItem { get; set; } = null!;

    public virtual LoadTransfer LoadTransfer { get; set; } = null!;
}
