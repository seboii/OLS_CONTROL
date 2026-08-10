using System;
using System.Collections.Generic;

namespace OLS.DataAccess.Entities;

public partial class AccountTypeMapping
{
    public long Id { get; set; }

    public int? AccountId { get; set; }

    public int? AccountTypeId { get; set; }

    public string? SiberId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
