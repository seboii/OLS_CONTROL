namespace OLS.DataAccess.Entities;

/// <summary>
/// NAVLUN ALIŞ KALEMİ → SATIŞ KALEMİ eşleşmesi ve kâr oranı.
///
/// Teklif/yük formunda bir navlun ALIŞ kalemi girildiğinde, karşılığı olan
/// SATIŞ satırı otomatik açılıp fiyatı <c>alış × (1 + MarkupPercent/100)</c>
/// olarak doldurulur.
///
/// NEDEN AYRI TABLO: eşleşme Siber'de HİÇBİR YERDE tutulmuyor —
/// <c>skn_kalem.alisfaturaiskalemid</c> 47.192 kalemin tamamında boş,
/// <c>skn_kalemdefault</c> tablosu boş, grup sütunları da öyle. Eşleşme
/// yalnızca kullanım geçmişinden çıkarılabiliyor, dolayısıyla burada
/// TANIMLANMASI gerekiyor.
///
/// ADDAN TÜRETİLEMEZ. "GİDERİ → GELİRİ" biçiminde basit bir ad çevirisi en
/// çok kullanılan çiftte SESSİZCE ÇALIŞMAZ: kara navlununun gelir karşılığı
/// "KARA NAVLUN GELİRİ" DEĞİL, <b>"KARA NAVLUN HİZMET BEDELİ"</b>dir ve
/// "KARA NAVLUN GELİRİ" diye bir kalem hiç yoktur. Eşleşmeler bu yüzden tek
/// tek, gerçek kullanımdan ölçülerek tanımlandı (bkz. seed migrasyonu).
/// </summary>
public partial class FinancialItemPair
{
    public long Id { get; set; }

    /// <summary>Alış kalemi (<c>load_financial_items.buysell = 1</c> ile girilen).</summary>
    public long PurchaseItemId { get; set; }

    /// <summary>Otomatik açılacak satış kalemi (<c>buysell = 2</c>).</summary>
    public long SaleItemId { get; set; }

    /// <summary>
    /// Alışın üzerine eklenecek kâr yüzdesi. Varsayılan 15.
    ///
    /// %15 YENİ BİR KURALDIR, mevcut alışkanlık değil: gerçek verideki kâr
    /// oranının medyanı kara'da 1,18, hava'da 1,10, deniz'de 1,22 ve tam 1,15
    /// olan yalnızca 43 kayıt var. Çift başına saklanıyor ki ileride oran
    /// dağıtım gerektirmeden ayarlanabilsin.
    /// </summary>
    public decimal MarkupPercent { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual FinancialItem PurchaseItem { get; set; } = null!;

    public virtual FinancialItem SaleItem { get; set; } = null!;
}
