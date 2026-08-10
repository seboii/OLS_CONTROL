using System;
using System.Collections.Generic;

namespace OLS.DataAccess.Entities;

public partial class LoadFinancialItem
{
    public long Id { get; set; }

    public long LoadId { get; set; }

    public int? Buysell { get; set; }

    public int? ItemTypeId { get; set; }

    public int? Item { get; set; }

    public int? AccountId { get; set; }

    public int? Quantity { get; set; }

    public string? Description { get; set; }

    public int? TransportTypeId { get; set; }

    public int? Status { get; set; }

    public int? Order { get; set; }

    public decimal? NetPrice { get; set; }

    public decimal? TaxPrice { get; set; }

    public decimal? TotalPrice { get; set; }

    public string? SiberId { get; set; }

    public int? Currency { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Load Load { get; set; } = null!;
}
