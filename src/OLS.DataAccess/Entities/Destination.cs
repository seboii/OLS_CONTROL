using System;
using System.Collections.Generic;

namespace OLS.DataAccess.Entities;

public partial class Destination
{
    public long Id { get; set; }

    public string Name { get; set; } = null!;

    public Guid CountryId { get; set; }

    public Guid CityId { get; set; }

    public Guid? DistrictId { get; set; }

    public string? Address { get; set; }

    public bool Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual City City { get; set; } = null!;

    public virtual Country Country { get; set; } = null!;

    public virtual District? District { get; set; }

    public virtual ICollection<ExpeditionMovement> ExpeditionMovements { get; set; } = new List<ExpeditionMovement>();

    public virtual ICollection<LoadTransferMovement> LoadTransferMovements { get; set; } = new List<LoadTransferMovement>();
}
