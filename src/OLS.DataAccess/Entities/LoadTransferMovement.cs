using System;
using System.Collections.Generic;

namespace OLS.DataAccess.Entities;

public partial class LoadTransferMovement
{
    public long Id { get; set; }

    public long LoadId { get; set; }

    public long? LoadTransferId { get; set; }

    public long DestinationId { get; set; }

    public string? Description { get; set; }

    public long UserId { get; set; }

    public string? Address { get; set; }

    public long ExpeditionStatusId { get; set; }

    public long? ExpeditionMovementId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual Destination Destination { get; set; } = null!;

    public virtual ExpeditionMovement? ExpeditionMovement { get; set; }

    public virtual ExpeditionStatus ExpeditionStatus { get; set; } = null!;

    public virtual Load Load { get; set; } = null!;

    public virtual LoadTransfer? LoadTransfer { get; set; }

    public virtual User User { get; set; } = null!;
}
