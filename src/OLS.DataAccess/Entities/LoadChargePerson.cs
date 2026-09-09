using System;
using System.Collections.Generic;

namespace OLS.DataAccess.Entities;

public partial class LoadChargePerson
{
    public long Id { get; set; }

    public int? LoadId { get; set; }

    public int? UserId { get; set; }

    /// <summary>
    /// 1: Operasyon Yetkilisi, 2: Satış Temsilcisi, 3: Fiyatlandıran.
    ///
    /// Siber'in <c>skn_rezervasyon</c> karşılıkları ve canlı doluluk
    /// (19.561 teklif) — hangi rolün GERÇEKTEN kullanıldığı buradan ölçüldü:
    ///
    /// <list type="table">
    /// <item><term>1 (ilk satır)</term><description><c>musteritemsilcisi</c> — kullanıcı ADI, 17.628 (%90)</description></item>
    /// <item><term>1 (ikinci satır)</term><description><c>operasyonyetkilisikod2</c> — kullanıcı KODU, 17.135 (%88)</description></item>
    /// <item><term>2</term><description><c>satistemsilcisikod</c> — kullanıcı KODU, 17.586 (%90)</description></item>
    /// <item><term>3</term><description><c>fiyatlandirankullaniciid</c> — kullanıcı GUID'i, 7.952 (%41; son 12 ayın %64'ü)</description></item>
    /// </list>
    ///
    /// KULLANILMAYAN ROL: <c>satistemsilcisi2kod</c> ("2. satış temsilcisi")
    /// 19.561 teklifin yalnızca 540'ında dolu (%2,8) — uygulamada karşılığı
    /// bilinçli olarak YOK. Aynısı yükte de geçerli (317/8.043).
    /// </summary>
    public int? UserType { get; set; }

    public string? SiberId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
