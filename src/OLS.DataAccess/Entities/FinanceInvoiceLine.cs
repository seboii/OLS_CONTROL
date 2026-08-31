using System;
using System.Collections.Generic;

namespace OLS.DataAccess.Entities;

/// <summary>
/// Fatura satırı — Siber <c>sfy_gelirgiderdetay</c> aynası.
///
/// <see cref="FinancialItemId"/> Siber'deki <c>kalemid</c>; yükün finans
/// kalemleriyle (sfy_modulkalem) aynı tanım kümesinden gelir, bu yüzden
/// fatura satırı ile yükün kalemi aynı adı taşır.
/// </summary>
public partial class FinanceInvoiceLine
{
    public long Id { get; set; }

    public string SiberId { get; set; } = null!;

    public long FinanceInvoiceId { get; set; }

    /// <summary>Mali kalem tanımı (Siber <c>kalemid</c>).</summary>
    public string? FinancialItemId { get; set; }

    public string? FinancialItemName { get; set; }

    public decimal? Quantity { get; set; }

    public decimal? UnitPrice { get; set; }

    public string? CurrencyCode { get; set; }

    public decimal? ExchangeRate { get; set; }

    public decimal? TaxRate { get; set; }

    public decimal? Amount { get; set; }

    public decimal? TaxAmount { get; set; }

    public decimal? AmountTl { get; set; }

    public decimal? TaxAmountTl { get; set; }

    public string? Description { get; set; }

    public string? DocumentNumber { get; set; }

    public DateTime? DocumentDate { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual FinanceInvoice FinanceInvoice { get; set; } = null!;
}
