using System;
using System.Collections.Generic;

namespace OLS.DataAccess.Entities;

public partial class AccountContactPerson
{
    public long Id { get; set; }

    public int? AccountId { get; set; }

    public string? Name { get; set; }

    public string? Email { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
