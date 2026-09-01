using System;
using System.Collections.Generic;

namespace OLS.DataAccess.Entities;

/// <summary>
/// Siber'in kendi değişiklik günlüğü (<c>sbr_log</c>) aynası — bir kaydın TAM
/// işlem geçmişi.
///
/// <c>skn_rezervasyon</c>/<c>skn_yuk</c>/<c>skn_pozisyon</c> üzerindeki
/// <c>insuser</c>/<c>upduser</c> alanları yalnızca İKİ noktayı verir (açan ve
/// en son dokunan). Aradaki her işlem bu tabloda: kim, ne zaman, hangi alanı,
/// hangi değerden hangi değere.
///
/// DEĞERLER SATIR SATIR HİZALI: <see cref="Fields"/>, <see cref="OldValues"/>
/// ve <see cref="NewValues"/> satır sonlarıyla ayrılmış ve konum konum
/// eşleşen listelerdir. Canlıda 3.000 örneğin 2.984'ünde (%99,5) hizalama
/// tutuyor; tutmayan %0,5 çok satırlı bir metin alanı içeriyor. Bu yüzden ham
/// metin saklanır ve eşleştirme OKUMA sırasında, hizalama doğrulanarak yapılır
/// — hizalama bozuksa yanlış "önceki → sonraki" çifti üretmek yerine
/// eşleştirme yapılmaz.
/// </summary>
public partial class SiberChangeLog
{
    public long Id { get; set; }

    public string SiberId { get; set; } = null!;

    /// <summary>Kaydın bulunduğu Siber tablosu (skn_yuk, skn_rezervasyon…).</summary>
    public string TableName { get; set; } = null!;

    /// <summary>Değişen kaydın Siber kimliği.</summary>
    public string RecordId { get; set; } = null!;

    /// <summary>Siber kullanıcı kodu.</summary>
    public string? UserCode { get; set; }

    public long? UserId { get; set; }

    public DateTime? ChangedAt { get; set; }

    /// <summary>1 ekleme, 2 güncelleme, 3 silme.</summary>
    public short? Operation { get; set; }

    /// <summary>Satır sonlarıyla ayrılmış alan adları (Türkçe etiketler).</summary>
    public string? Fields { get; set; }

    public string? OldValues { get; set; }

    public string? NewValues { get; set; }

    /// <summary>Siber'in kayda verdiği okunur etiket (yük no, teklif no…).</summary>
    public string? RecordLabel { get; set; }

    public string? Module { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User? User { get; set; }
}
