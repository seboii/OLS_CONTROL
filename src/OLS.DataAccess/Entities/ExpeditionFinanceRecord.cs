using System;

namespace OLS.DataAccess.Entities;

public partial class ExpeditionFinanceRecord
{
    public long Id { get; set; }

    public Guid SiberId { get; set; }

    public long? ExpeditionId { get; set; }

    public long? LoadTransferId { get; set; }

    public string? ExpeditionNumber { get; set; }

    public string? LoadNumber { get; set; }

    public string? ItemName { get; set; }

    public string? Description { get; set; }

    public DateOnly? DocumentDate { get; set; }

    public decimal ExpectedIncomeTry { get; set; }

    public decimal ExpectedExpenseTry { get; set; }

    public decimal RealizedIncomeTry { get; set; }

    public decimal RealizedExpenseTry { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Expedition? Expedition { get; set; }

    public virtual LoadTransfer? LoadTransfer { get; set; }
}
