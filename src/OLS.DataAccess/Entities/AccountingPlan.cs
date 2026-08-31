using System;
using System.Collections.Generic;

namespace OLS.DataAccess.Entities;

/// <summary>
/// Hesap planı — Siber <c>sfy_hesapplan</c> aynası.
///
/// Hesap kodu boşlukla ayrılmış hiyerarşik bir metindir ("100 01 01 0001") ve
/// <see cref="Level"/> 1..4 arasında değişir; 4. seviye yaprak hesaptır
/// (3.287 / 3.938). Kod, fiş satırlarıyla METİN üzerinden eşleşir — Siber'de
/// hesap planına giden bir yabancı anahtar yoktur.
/// </summary>
public partial class AccountingPlan
{
    public long Id { get; set; }

    public string SiberId { get; set; } = null!;

    /// <summary>Hiyerarşik hesap kodu ("100 01 01 0001").</summary>
    public string Code { get; set; } = null!;

    public string? Name { get; set; }

    public string? Name2 { get; set; }

    /// <summary>1..4; 4 yaprak hesap.</summary>
    public short? Level { get; set; }

    public bool IsPassive { get; set; }

    /// <summary>Siber şirket kimliği — kapsam kısıtı için (bkz. CompanyScope).</summary>
    public string? SiberCompanyId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
