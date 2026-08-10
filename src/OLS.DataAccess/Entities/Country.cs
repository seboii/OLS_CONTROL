using System;
using System.Collections.Generic;

namespace OLS.DataAccess.Entities;

public partial class Country
{
    public Guid Id { get; set; }

    public string? Name { get; set; }

    public string? CountryCode { get; set; }

    public string? Flag { get; set; }

    public string? PhoneCode { get; set; }

    public string? Slug { get; set; }

    public string? SiberId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Destination> Destinations { get; set; } = new List<Destination>();
}
