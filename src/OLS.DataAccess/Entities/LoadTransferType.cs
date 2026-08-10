using System;
using System.Collections.Generic;

namespace OLS.DataAccess.Entities;

public partial class LoadTransferType
{
    public long Id { get; set; }

    public string? Name { get; set; }

    public string? Code { get; set; }

    public string? GroupCode { get; set; }

    public string? SiberId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
