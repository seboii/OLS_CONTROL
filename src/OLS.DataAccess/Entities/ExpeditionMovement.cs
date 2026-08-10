using System;
using System.Collections.Generic;

namespace OLS.DataAccess.Entities;

public partial class ExpeditionMovement
{
    public long Id { get; set; }

    public long? ExpeditionId { get; set; }

    public long DestinationId { get; set; }

    public string? Description { get; set; }

    public long UserId { get; set; }

    public string? Address { get; set; }

    public long ExpeditionStatusId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual Destination Destination { get; set; } = null!;

    public virtual Expedition? Expedition { get; set; }

    public virtual ExpeditionStatus ExpeditionStatus { get; set; } = null!;

    public virtual ICollection<LoadTransferMovement> LoadTransferMovements { get; set; } = new List<LoadTransferMovement>();

    public virtual User User { get; set; } = null!;
}
