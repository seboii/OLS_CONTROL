using System;
using System.Collections.Generic;

namespace OLS.DataAccess.Entities;

public partial class Account
{
    public long Id { get; set; }

    public string? Name { get; set; }

    public string? TaxNumber { get; set; }

    public string? TaxOffice { get; set; }

    public string? AccountingCode { get; set; }

    public int? InvoiceType { get; set; }

    public Guid? CountryId { get; set; }

    public Guid? CityId { get; set; }

    public Guid? DistrictId { get; set; }

    public string? Address { get; set; }

    public string? Phone { get; set; }

    public Guid? PhoneCountryId { get; set; }

    public string? Avatar { get; set; }

    public string? Email { get; set; }

    public string? ContactPerson { get; set; }

    public string? IndividualPersonal { get; set; }

    public int Discount { get; set; }

    public string? SiberId { get; set; }

    public string? ContactLanguage { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
