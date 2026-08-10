using System;
using System.Collections.Generic;

namespace OLS.DataAccess.Entities;

public partial class LoadTransferPackage
{
    public long Id { get; set; }

    public string? Yukkoliid { get; set; }

    public string? LoadTransferId { get; set; }

    public int? Quantity { get; set; }

    public string? CaseTypeId { get; set; }

    public decimal? Width { get; set; }

    public decimal? Length { get; set; }

    public decimal? Height { get; set; }

    public decimal? Volume { get; set; }

    public decimal? GrossWeight { get; set; }

    public decimal? NetWeight { get; set; }

    public decimal? Lademeter { get; set; }

    public int? Stackable { get; set; }

    public int? ProductTypeId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
