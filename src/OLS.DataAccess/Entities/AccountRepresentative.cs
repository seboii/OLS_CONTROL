namespace OLS.DataAccess.Entities;

/// <summary>
/// Cariye (müşteriye) bağlı görevliler — Siber'deki <c>sbr_firmatemsilci</c>
/// tablosunun yerel karşılığı.
///
/// Amaç: teklif açarken müşteri seçilince "Görevliler" sekmesindeki Operasyon
/// Yetkilisi ve Satış Temsilcisi alanlarının kendiliğinden dolması (kullanıcı
/// isteği). Siber'de bu bağ firma başına bir/birkaç satırla tutuluyor; kişi
/// <c>kod</c> (kullanıcının siber kodu) ile belirtiliyor ve rolü iki bayrak
/// söylüyor: <c>satistemsilcisi</c> ve <c>operasyonyetkilisi</c>.
///
/// BİLİNÇLİ OLARAK <c>user_account_mappings</c> KULLANILMADI: o tablo yetki
/// filtrelemesinde kullanılıyor (kullanıcı yalnızca kendisine atanmış carilerin
/// tekliflerini görebiliyor — bkz. LoadService.ListAsync). Oraya 4000+ satır
/// yazmak, istenmemiş bir görünürlük genişlemesine yol açardı.
/// </summary>
public partial class AccountRepresentative
{
    public long Id { get; set; }

    public int AccountId { get; set; }

    public int UserId { get; set; }

    /// <summary>1 = Operasyon Yetkilisi, 2 = Satış Temsilcisi (load_charge_people ile aynı kodlama).</summary>
    public int UserType { get; set; }

    /// <summary>Siber'deki <c>sbr_firmatemsilci.firmatemsilciid</c> — senkron anahtarı.</summary>
    public string? SiberId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
