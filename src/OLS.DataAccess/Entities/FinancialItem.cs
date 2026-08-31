using System;
using System.Collections.Generic;

namespace OLS.DataAccess.Entities;

public partial class FinancialItem
{
    public long Id { get; set; }

    public string Name { get; set; } = null!;

    public int? Type { get; set; }

    /// <summary>
    /// Bu kalem girildiğinde varsayılan olarak önerilecek cari (firma).
    ///
    /// Siber'de kalem↔firma için bir TANIM tablosu yoktur (skn_kalemdefault boş ve
    /// firma sütunu bile taşımıyor) — ilişki KULLANIM geçmişinden çıkarılır:
    /// sfy_modulkalem'de bir kalemin satırlarının ezici çoğunluğu tek bir firmaya
    /// aitse o firma varsayılan kabul edilir. Canlıda ölçüldü: 37 kalem %70+
    /// baskınlık gösteriyor (GÜMRÜK VERGİSİ %96, BELGESİZ GİDERLER %99),
    /// 51 kalem ise dağınık (KARA NAVLUN HİZMET BEDELİ 436 farklı firma, %14) —
    /// yani "bazıları firmayla ilişkili" gözlemi veriyle birebir örtüşüyor.
    /// Dağınık kalemlerde NULL kalır ve arayüz hiçbir şey doldurmaz.
    /// </summary>
    public long? DefaultAccountId { get; set; }

    /// <summary>
    /// <see cref="DefaultAccountId"/>'nin adı — BİLİNÇLİ olarak denormalize edildi.
    ///
    /// Kalem listesi (LookupService) generic bir servistir ve Include/DTO projeksiyonu
    /// yapmaz; adı ayrı bir istekle çekmek ise cari uçlarının yetki kontrolüne
    /// (account_management + cariye görünürlük) takılırdı — teklif girenlerde bu
    /// yetki olmayabilir. Zaten türetilmiş bir önbellek sütunu olduğu için her
    /// senkronda id ile birlikte tazelenir.
    /// </summary>
    public string? DefaultAccountName { get; set; }

    public string? SiberId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
