using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OLS.DataAccess.Migrations
{
    /// <summary>
    /// YÜK MALİ KALEMLERİ İKİ KEZ DURUYORDU.
    ///
    /// Yerel tabloda 76.143 satır vardı ama bunlar yalnızca 38.588 farklı
    /// Siber kalemine (<c>sfy_modulkalem.modulkalemid</c>) işaret ediyordu:
    /// 37.555 kalem TAM İKİ KEZ kayıtlıydı. Kopyalar 2026-08-18 ve 2026-08-25
    /// tarihli iki ayrı toplu aktarımdan geliyor — o günkü senkron kaydı
    /// Siber kimliğine göre eşleştirmiyor, her turda yeniden EKLİYORDU.
    ///
    /// Kullanıcının gördüğü: yükün Finans bölümünde her satır iki kez
    /// listeleniyor ve alış/satış toplamları iki katına çıkıyordu — yani ekran
    /// Siber'in gerçek verisini göstermiyordu.
    ///
    /// HANGİ KOPYA KALIR: en KÜÇÜK kimlikli olan. Senkron
    /// (<c>ExistingByKeyAsync</c> → <c>OrderBy(id)</c> + <c>TryAdd</c>) zaten
    /// o satırı buluyor ve her turda Siber'den tazeliyor; yüksek kimlikli
    /// kopyalar 2026-08-25'te donmuş durumda. 37.555 grubun 12.378'inde iki
    /// kopyanın içeriği FARKLI ve farkın sebebi tam olarak bu: güncellenen
    /// satır doğru, donmuş olan eski.
    ///
    /// Bugünkü senkron kodu artık <c>modulkalemid</c> üzerinden eşleştiriyor,
    /// yani temizlik sonrası kopya yeniden oluşmuyor.
    ///
    /// Yedek: <c>storage/backups/yuk-mali-kalem-mukerrer-20260909.csv</c>
    /// (silinen 37.555 satırın tamamı).
    /// </summary>
    public partial class RemoveDuplicateLoadInvoiceItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM load_transfer_invoice_items i
                USING (
                    SELECT min(id) AS keep_id, lower(modulkalemid) AS key
                    FROM load_transfer_invoice_items
                    WHERE modulkalemid IS NOT NULL
                    GROUP BY lower(modulkalemid)
                ) k
                WHERE lower(i.modulkalemid) = k.key
                  AND i.id <> k.keep_id;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // GERİ ALINAMAZ. Silinen satırlar Siber'in aynısıydı ve kopyaydı;
            // geri yüklemek hatayı geri getirirdi. Gerçekten gerekirse yedek
            // CSV'den elle alınır.
        }
    }
}
