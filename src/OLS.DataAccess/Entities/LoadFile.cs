using System;
using System.Collections.Generic;

namespace OLS.DataAccess.Entities;

public partial class LoadFile
{
    public long Id { get; set; }

    public int? LoadId { get; set; }

    public string? File { get; set; }

    public string? MimeType { get; set; }

    public string? OrgName { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
