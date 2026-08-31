using System;
using System.Collections.Generic;

namespace OLS.DataAccess.Entities;

/// <summary>
/// skn_yukevrak karşılığı — fiziksel evrak çeklisti (gerçek dosya/ek DEĞİL):
/// hangi evrak türünden kaç orijinal/kopya çıkarıldığı ve kime/ne zaman
/// teslim edildiği. Bkz. EvrakTuru.
/// </summary>
public partial class LoadTransferDocument
{
    public long Id { get; set; }

    public string? Yukevrakid { get; set; }

    public long LoadTransferId { get; set; }

    public long? EvrakTuruId { get; set; }

    public string? DocumentNumber { get; set; }

    public DateOnly? Date { get; set; }

    public int? OriginalCount { get; set; }

    public int? CopyCount { get; set; }

    public string? DeliveredTo { get; set; }

    public DateOnly? DeliveredAt { get; set; }

    public string? Note { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
