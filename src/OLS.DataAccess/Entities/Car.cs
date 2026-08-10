using System;
using System.Collections.Generic;

namespace OLS.DataAccess.Entities;

public partial class Car
{
    public long Id { get; set; }

    public string? SiberId { get; set; }

    public string? PlateNumber { get; set; }

    public int? CarType { get; set; }

    public int? RomorkType { get; set; }

    public int? VehicleOwner { get; set; }

    public int? VehicleStatus { get; set; }

    public string? CustomerId { get; set; }

    public double? Km { get; set; }

    public int? InCountry { get; set; }

    public int? International { get; set; }

    public double? Width { get; set; }

    public double? Length { get; set; }

    public double? Height { get; set; }

    public double? Capacity { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
