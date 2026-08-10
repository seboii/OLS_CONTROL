using System;
using System.Collections.Generic;

namespace OLS.DataAccess.Entities;

public partial class LoadTransferInvoiceItem
{
    public long Id { get; set; }

    public string? Modulkalemid { get; set; }

    public string? Modulid { get; set; }

    public string? Modulkod { get; set; }

    public int? ItemId { get; set; }

    public string? Buysell { get; set; }

    public int? AccountId { get; set; }

    public decimal? TotalPrice { get; set; }

    public int? CurrencyCode { get; set; }

    public decimal? NetPrice { get; set; }

    public decimal? Quantity { get; set; }

    public decimal? TaxPrice { get; set; }

    public decimal? TaxRate { get; set; }

    public string? InsertName { get; set; }

    public string? Description { get; set; }

    public int? UserId { get; set; }

    public int? TransferredFromReservation { get; set; }

    /// <summary>
    /// pending, invoice_received, invoice_issued
    /// </summary>
    public string Status { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<LoadTransferInvoiceMap> LoadTransferInvoiceMaps { get; set; } = new List<LoadTransferInvoiceMap>();
}
