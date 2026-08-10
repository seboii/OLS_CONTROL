using System;
using System.Collections.Generic;

namespace OLS.DataAccess.Entities;

public partial class LoadChargePerson
{
    public long Id { get; set; }

    public int? LoadId { get; set; }

    public int? UserId { get; set; }

    /// <summary>
    /// 1: Operasyon Yetkilisi, 2: Satış Temsilcisi
    /// </summary>
    public int? UserType { get; set; }

    public string? SiberId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
