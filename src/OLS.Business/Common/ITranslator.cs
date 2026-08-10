namespace OLS.Business.Common;

/// <summary>
/// Laravel'in <c>__('general.…')</c> yardımcısının karşılığı.
///
/// Dikkat: olsold'da çeviri anahtarı Türkçe cümlenin kendisidir
/// (ör. <c>__('general.Kayıt Başarılı')</c>). Bu yapıyı koruyoruz ki
/// mevcut mesaj metinleri birebir aynı kalsın.
/// </summary>
public interface ITranslator
{
    /// <summary>
    /// Anahtarın aktif dildeki karşılığını döndürür.
    /// Karşılık bulunamazsa anahtarın kendisi döner — Laravel de böyle davranır.
    /// </summary>
    string Get(string key);
}
