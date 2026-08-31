using System;
using System.Collections.Generic;

namespace OLS.DataAccess.Entities;

/// <summary>
/// skn_yukevrak.sirano için 10 sabit değer (gerçek Siber'de doğrulandı):
/// 1=Navlun Faturası, 2=Invoice, 3=Konşimento, 4=CMR, 5=Mal Faturası, 6=ATR-1,
/// 7=Packing List, 8=Sağlık Sertifikası, 9=Çeki Listesi, 10=Menşei Şehadetnamesi.
/// Siber'de bu türler için ayrı bir GUID'li tanım tablosu yok — sirano doğrudan
/// skn_yukevrak satırında tutuluyor, bu yüzden burada SiberId yok; Code bizzat
/// o sirano değerini ("1".."10") taşır.
/// </summary>
public partial class EvrakTuru
{
    public long Id { get; set; }

    public string Name { get; set; } = null!;

    public string Code { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
