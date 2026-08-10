using System;
using System.Collections.Generic;

namespace OLS.DataAccess.Entities;

public partial class WebsiteContactForm
{
    public long Id { get; set; }

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? Phone { get; set; }

    public string Message { get; set; } = null!;

    public bool IsRead { get; set; }

    public bool IsAnswered { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
