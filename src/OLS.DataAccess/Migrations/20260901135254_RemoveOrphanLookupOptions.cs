using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OLS.DataAccess.Migrations
{
    /// <summary>
    /// Siber'de karşılığı OLMAYAN tanım seçeneklerini kaldırır.
    ///
    /// NEDEN: teklifsiz yük formundaki açılır listeler yerel tablolardan
    /// besleniyor, ama kayıt Siber'e yazılıyor. Karşılığı olmayan bir seçenek
    /// seçildiğinde INSERT Siber tarafında düşüyor ve kullanıcı ekranda yalnızca
    /// "beklenmeyen bir hata oluştu" görüyordu. En sert olanı departman:
    /// <c>skn_yuk.departmanid</c> FK'li (FK_skn_yuk_sbr_departman_departmanid),
    /// yani sahte departman seçimi INSERT'i kesin olarak düşürüyor.
    ///
    /// Kaldırılanlar, taklit Siber'den (infra/docker/siber-mock) gerçek
    /// veritabanına sızmış kayıtlar ve DbSeeder'ın karşılığı doğrulanmamış
    /// başlangıç satırları:
    ///
    ///   departments    Operasyon / Satış / Muhasebe (88888888-…)
    ///                  Gerçek <c>sbr_departman</c> 7 satır ve yereldeki diğer
    ///                  7 satırla birebir eşleşiyor; bu üçünün karşılığı yok.
    ///   payment_types  Vadeli (77777777-…)
    ///   product_types  Test Ürün Grubu (siber_id boş)
    ///   loading_types  PARSİYEL (siber_id "ref-yuklemetip-0")
    ///
    /// PARSİYEL ÖZEL DURUM — silinmiyor, TAŞINIYOR: 9.501 teklif bu satıra
    /// bağlıydı. Siber'in <c>skn_sabittanim</c>(YUKLEMETIP) listesi tam olarak
    /// üç satır: GRUPAJ(0) / KOMPLE(1) / CO-LOAD(2). Parsiyel ile grupaj aynı
    /// şey (LTL / konsolide kısmi yük) ve Siber'in kullandığı ad GRUPAJ —
    /// Siber'de kod 0 ile 4.067 yük, 9.500 teklif kayıtlı. Daha kötüsü:
    /// yereldeki PARSİYEL de kod "0" taşıyordu, yani GRUPAJ ile AYNI kod.
    /// Senkron eşlemesi koda göre sözlük kurduğu için (ByCode, ilk gelen kazanır
    /// ve satır sırası garanti değil) Siber'in GRUPAJ kayıtları yerelde bu iki
    /// satırdan hangisine düşeceği rastgeleydi. Kayıtlar gerçek karşılıkları
    /// GRUPAJ'a taşınıp yinelenen satır kaldırıldı.
    ///
    /// Yedek: storage/backups/oksuz-tanimlar-yedegi-20260901.csv
    /// </summary>
    public partial class RemoveOrphanLookupOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Kimlikler ortamdan ortama değiştiği için her eşleşme ADA ya da
            // taklit veriden gelen GUID ÖNEKİNE göre yapılır.

            // --- Yükleme tipi: PARSİYEL → GRUPAJ ---------------------------
            migrationBuilder.Sql("""
                UPDATE loads SET loading_type_id = (
                        SELECT id FROM loading_types WHERE upper(name) = 'GRUPAJ' LIMIT 1)
                WHERE loading_type_id IN (
                        SELECT id FROM loading_types WHERE upper(name) = 'PARSİYEL')
                  AND EXISTS (SELECT 1 FROM loading_types WHERE upper(name) = 'GRUPAJ');

                DELETE FROM loading_types
                WHERE upper(name) = 'PARSİYEL'
                  AND EXISTS (SELECT 1 FROM loading_types WHERE upper(name) = 'GRUPAJ');
                """);

            // --- Departman: taklit Siber'den sızan 3 satır ------------------
            // Departman alanı NULL kabul ediyor; bağlı kayıt silinmez.
            migrationBuilder.Sql("""
                UPDATE load_transfers SET department_id = NULL
                WHERE department_id IN (SELECT id FROM departments WHERE siber_id LIKE '88888888-%');

                UPDATE load_transfers SET operation_department_id = NULL
                WHERE operation_department_id IN (SELECT id FROM departments WHERE siber_id LIKE '88888888-%');

                UPDATE loads SET department_id = NULL
                WHERE department_id IN (SELECT id FROM departments WHERE siber_id LIKE '88888888-%');

                UPDATE expeditions SET department_id = NULL
                WHERE department_id IN (SELECT id FROM departments WHERE siber_id LIKE '88888888-%');

                DELETE FROM departments WHERE siber_id LIKE '88888888-%';
                """);

            // --- Ödeme tipi: karşılığı hiç verilmemiş "Vadeli" --------------
            migrationBuilder.Sql("""
                UPDATE loads SET payment_type_id = NULL
                WHERE payment_type_id IN (SELECT id FROM payment_types WHERE siber_id LIKE '77777777-%');

                UPDATE load_transfers SET payment_type_id = NULL
                WHERE payment_type_id IN (SELECT id FROM payment_types WHERE siber_id LIKE '77777777-%');

                DELETE FROM payment_types WHERE siber_id LIKE '77777777-%';
                """);

            // --- Ürün grubu: siber_id'si hiç olmayan test satırı ------------
            migrationBuilder.Sql("""
                UPDATE load_transfer_packages SET product_type_id = NULL
                WHERE product_type_id IN (
                        SELECT id FROM product_types
                        WHERE siber_id IS NULL AND name = 'Test Ürün Grubu');

                DELETE FROM product_types
                WHERE siber_id IS NULL AND name = 'Test Ürün Grubu';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Geri alınmaz: silinen satırların Siber'de karşılığı yok, geri
            // getirmek yük oluşturmayı yeniden bozardı. Gerekirse yedekten
            // (storage/backups/oksuz-tanimlar-yedegi-20260901.csv) elle alınır.
        }
    }
}
