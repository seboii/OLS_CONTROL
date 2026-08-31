using System;
using System.Collections.Generic;

namespace OLS.DataAccess.Entities;

/// <summary>
/// Muhasebe fişi (yevmiye) — Siber <c>sfy_fis</c> aynası.
///
/// <see cref="VoucherType"/> Siber'de neredeyse tamamen 3'tür (54.758 / 54.849);
/// 0/1/2 türleri toplam 91 kayıtla sınırlı, bu yüzden tür üzerinden ekran
/// ayrımı yapılmaz, tür yalnızca gösterilir.
/// </summary>
public partial class FinanceVoucher
{
    public long Id { get; set; }

    public string SiberId { get; set; } = null!;

    public short? VoucherType { get; set; }

    public DateTime? VoucherDate { get; set; }

    public int? VoucherNumber { get; set; }

    /// <summary>Yevmiye numarası — resmi deftere işlenen sıra.</summary>
    public int? JournalNumber { get; set; }

    public string? Description { get; set; }

    public string? CurrencyCode { get; set; }

    public string? DocumentNumber { get; set; }

    public DateTime? DocumentDate { get; set; }

    public bool IsChecked { get; set; }

    public string? SiberCompanyId { get; set; }

    public DateTime? SiberCreatedAt { get; set; }

    public string? SiberCreatedBy { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<FinanceVoucherLine> Lines { get; set; } = new List<FinanceVoucherLine>();
}
