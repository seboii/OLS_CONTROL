using System;
using System.Collections.Generic;

namespace OLS.DataAccess.Entities;

public partial class ExpeditionStatus
{
    public long Id { get; set; }

    public int? ExpeditionStatusId { get; set; }

    public string? Name { get; set; }

    public int? LoadStatusId { get; set; }

    public string? Rowguid { get; set; }

    public int? OrderNumber { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<ExpeditionMovement> ExpeditionMovements { get; set; } = new List<ExpeditionMovement>();

    public virtual ICollection<LoadTransferMovement> LoadTransferMovements { get; set; } = new List<LoadTransferMovement>();
}
