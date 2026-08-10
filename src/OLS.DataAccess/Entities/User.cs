using System;
using System.Collections.Generic;

namespace OLS.DataAccess.Entities;

public partial class User
{
    public long Id { get; set; }

    public string Name { get; set; } = null!;

    public string Surname { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? Phone { get; set; }

    public Guid? CountryId { get; set; }

    public Guid? PhoneCountryId { get; set; }

    public DateTime? EmailVerifiedAt { get; set; }

    public string? Password { get; set; }

    public string? Avatar { get; set; }

    public bool NotificationMail { get; set; }

    public bool NotificationSms { get; set; }

    public bool Status { get; set; }

    public string? SiberId { get; set; }

    public string? SiberName { get; set; }

    public string? SiberCode { get; set; }

    public string? EmailId { get; set; }

    public string? EmailPassword { get; set; }

    public bool WorkingTracking { get; set; }

    public string? PkdsId { get; set; }

    public string? RememberToken { get; set; }

    public DateTime? DeletedAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<ExpeditionMovement> ExpeditionMovements { get; set; } = new List<ExpeditionMovement>();

    public virtual ICollection<LoadTransferMovement> LoadTransferMovements { get; set; } = new List<LoadTransferMovement>();

    public virtual ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
}
