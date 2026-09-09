using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OLS.DataAccess.Migrations
{
    /// <summary>
    /// MÜKERRER "Türkiye" ÜLKESİ BİRLEŞTİRİLDİ.
    ///
    /// <c>countries</c> tablosunda Türkiye İKİ satırdı ve ikisi de AYNI Siber
    /// ülkesine (<c>sbr_ulke.ulkeid = EB3F2DBE-…</c>) bakıyordu:
    ///
    /// <list type="table">
    /// <item><term>EB3F2DBE-… "TÜRKİYE"</term><description>53 şehir, 2.587 cari, 4.325 teklif, 268 yük, 25 varış — GERÇEK kayıt</description></item>
    /// <item><term>E98225C2-… "Türkiye"</term><description>50 şehir, başka HİÇBİR kullanımı yok — fazlalık</description></item>
    /// </list>
    ///
    /// KÖKENİ: fazlalık satır <c>DbSeeder</c>'ın tohumladığı "Türkiye"dir.
    /// İçe aktarma o dönemde adları <c>ToUpperInvariant()</c> ile eşleştiriyordu
    /// ("Türkiye" → "TÜRKIYE" ≠ "TÜRKİYE"), bu yüzden Siber'in satırı eşleşemeyip
    /// İKİNCİ bir kayıt olarak açıldı. Eşleştirme bugün Türkçe katlamalı
    /// (bkz. <c>TurkishFold</c>) ve ikisini aynı anahtara indiriyor — yani
    /// fazlalık satır silindikten sonra bir daha OLUŞMAZ.
    ///
    /// ŞEHİRLER SİLİNMEZ, TAŞINIR. 50 şehrin hiçbirinin Siber kimliği doğru
    /// satırın 53 şehriyle ÇAKIŞMIYOR (ölçüldü: 0 ortak <c>siber_id</c>).
    /// Dördünün adı aynı (ZONGULDAK, SAMSUN, SİNOP, MERSİN) ama Siber'de bunlar
    /// AYRI <c>sehirid</c>'ler — ada göre birleştirmek daha önce 12 gerçek şehri
    /// listeden düşürmüştü (bkz. <c>RestoreCitiesMergedByName</c>), bu yüzden
    /// burada yalnızca ülke bağı değiştiriliyor, satır sayısı korunuyor.
    ///
    /// Yedekler: <c>storage/backups/mukerrer-turkiye-20260908.csv</c> ve
    /// <c>storage/backups/turkiye-sehir-yedegi-20260908.csv</c>.
    /// </summary>
    public partial class MergeDuplicateTurkiyeCountry : Migration
    {
        private const string Duplicate = "e98225c2-2434-488d-85d3-db583fdb0fce";
        private const string Real = "eb3f2dbe-96fe-4b17-9947-c0ad63af76ca";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // cities.country_id METİN sütunu (uuid değil) — karşılaştırma da öyle.
            migrationBuilder.Sql($"""
                UPDATE cities SET country_id = '{Real}'
                WHERE country_id = '{Duplicate}';
                """);

            // Diğer tüm bağlar migrasyondan ÖNCE ölçüldü ve hepsi SIFIR: cari,
            // kullanıcı, teklif, yük, varış. Beklenmedik bir bağ kalmışsa
            // buradaki DELETE yabancı anahtar hatasıyla düşer ve migrasyon
            // tamamen geri alınır — sessiz veri kaybı olmaz.
            migrationBuilder.Sql($"DELETE FROM countries WHERE id = '{Duplicate}'::uuid;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Satır geri gelir; ŞEHİR DAĞILIMI GERİ GELMEZ (hangi 50 şehrin bu
            // satıra bağlı olduğu yalnızca yedek CSV'de duruyor). Geri alma
            // gerekirse şehirleri o dosyadan yüklemek gerekir.
            migrationBuilder.Sql($"""
                INSERT INTO countries (id, name, country_code, phone_code, siber_id, created_at, updated_at)
                SELECT '{Duplicate}'::uuid, 'Türkiye', 'TR', '90',
                       'EB3F2DBE-96FE-4B17-9947-C0AD63AF76CA', NOW(), NOW()
                WHERE NOT EXISTS (SELECT 1 FROM countries WHERE id = '{Duplicate}'::uuid);
                """);
        }
    }
}
