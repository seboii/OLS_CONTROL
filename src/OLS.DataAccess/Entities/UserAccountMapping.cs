using System;
using System.Collections.Generic;

namespace OLS.DataAccess.Entities;

public partial class UserAccountMapping
{
    public long Id { get; set; }

    public int UserId { get; set; }

    public int AccountId { get; set; }

    public string? SiberId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
