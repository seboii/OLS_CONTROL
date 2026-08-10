using System;
using System.Collections.Generic;

namespace OLS.DataAccess.Entities;

public partial class TaxOffice
{
    public long Id { get; set; }

    public string? SiberId { get; set; }

    public string? Name { get; set; }

    public int? SpecialCode { get; set; }

    public string? City { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
