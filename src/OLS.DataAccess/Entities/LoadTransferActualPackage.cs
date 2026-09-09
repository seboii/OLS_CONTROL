namespace OLS.DataAccess.Entities;

/// <summary>
/// GERÇEK KOLİ BİLGİLERİ — Siber'de <c>skn_yukkolidepo</c>.
///
/// Siber'in yük ekranında koli bölümü İKİ sete ayrılıyor:
///   • "Yük Koli Bilgileri"   → <c>skn_yukkoli</c>   (bizde LoadTransferPackage)
///   • "Gerçek Koli Bilgileri" → <c>skn_yukkolidepo</c> (bu sınıf)
///
/// Aradaki "Gerçek Koli Bilgilerine Aktar" düğmesi beyan edilen kolileri
/// gerçek koli setine KOPYALIYOR; kullanıcı sonra depoda ölçülen değerlere göre
/// düzeltiyor. Canlı veri bunu birebir doğruluyor: iki seti de olan 4.064 yükün
/// <b>3.920'sinde (%96,5)</b> toplamlar birebir aynı (aktarılmış, dokunulmamış),
/// kalan ~144'ünde farklı (aktarıldıktan sonra düzeltilmiş).
///
/// Tablo <c>skn_yukkoli</c>'nin neredeyse birebir aynası. Siber'de tek ZORUNLU
/// sütun <c>yukid</c>; <c>kapid</c> ve <c>yukid</c> yabancı anahtarlı, üzerinde
/// <c>skn_yukkolidepo_kapaniskontrol</c> tetikleyicisi var (dönem kapanışı
/// kontrolü — kapalı dönemde yazma RAISERROR ile reddedilir ve mesaj
/// ExceptionHandlingMiddleware üzerinden kullanıcıya olduğu gibi gösterilir).
/// </summary>
public partial class LoadTransferActualPackage
{
    public long Id { get; set; }

    /// <summary>Siber'deki <c>skn_yukkolidepo.yukkolidepoid</c>.</summary>
    public string? Yukkolidepoid { get; set; }

    public string? LoadTransferId { get; set; }

    public int? Quantity { get; set; }

    public string? CaseTypeId { get; set; }

    public decimal? Width { get; set; }

    public decimal? Length { get; set; }

    public decimal? Height { get; set; }

    public decimal? Volume { get; set; }

    public decimal? GrossWeight { get; set; }

    public decimal? NetWeight { get; set; }

    public decimal? Lademeter { get; set; }

    public int? Stackable { get; set; }

    public int? ProductTypeId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
