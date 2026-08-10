using System;
using System.Collections.Generic;

namespace OLS.DataAccess.Entities;

public partial class LoadContent
{
    public long Id { get; set; }

    public long LoadId { get; set; }

    public int? ProductTypeId { get; set; }

    public int? CaseTypeId { get; set; }

    public int? Quantity { get; set; }

    public decimal? Width { get; set; }

    public decimal? Height { get; set; }

    public decimal? Length { get; set; }

    public decimal? GrossWeight { get; set; }

    public decimal? NetWeight { get; set; }

    public decimal? Volume { get; set; }

    public decimal? Lademeter { get; set; }

    public int? Stackable { get; set; }

    public string? SiberId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Load Load { get; set; } = null!;
}
