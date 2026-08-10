using System;
using System.Collections.Generic;

namespace OLS.DataAccess.Entities;

public partial class Invoice
{
    public long Id { get; set; }

    public long? InvoiceTypeId { get; set; }

    public long? InvoiceStatusId { get; set; }

    public long? AccountId { get; set; }

    /// <summary>
    /// 0 = Inbox, 1 = Outbox
    /// </summary>
    public short BoxType { get; set; }

    public string? InvoiceId { get; set; }

    public string? DocumentId { get; set; }

    /// <summary>
    /// 0 = Temel Fatura, 1 = Ticari Fatura
    /// </summary>
    public int CommercialType { get; set; }

    public string TargetIdentityNo { get; set; } = null!;

    public string TargetTitle { get; set; } = null!;

    public string? EnvelopeIdentifier { get; set; }

    public int? EnvelopeStatusCode { get; set; }

    public string? Message { get; set; }

    public DateTime InvoiceCreateDate { get; set; }

    public DateTime InvoiceExecutionDate { get; set; }

    public decimal? PayableAmount { get; set; }

    public decimal? TaxAmount { get; set; }

    public decimal? TaxExclusiveAmount { get; set; }

    public decimal? TaxRate { get; set; }

    public string? DocumentCurrencyCode { get; set; }

    public DateOnly? ExchangeDate { get; set; }

    public decimal? ExchangeRate { get; set; }

    public string? OrderDocumentId { get; set; }

    public bool IsArchived { get; set; }

    /// <summary>
    /// 0 = Manuel, 1 = Integration
    /// </summary>
    public bool CreatedByIntegration { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Account? Account { get; set; }

    public virtual ICollection<InvoiceFooter> InvoiceFooters { get; set; } = new List<InvoiceFooter>();

    public virtual InvoiceStatus? InvoiceStatus { get; set; }

    public virtual InvoiceType? InvoiceType { get; set; }

    public virtual ICollection<LoadTransferInvoiceMap> LoadTransferInvoiceMaps { get; set; } = new List<LoadTransferInvoiceMap>();
}
