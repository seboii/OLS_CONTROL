using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OLS.DataAccess.Migrations
{
    /// <summary>
    /// Taklit Siber'den (infra/docker/siber-mock) gerçek veritabanına sızmış
    /// KAYITLARI siler. Uygulama bir ara mock'a bağlıyken içe aktarılan bu
    /// satırlar, gerçek Siber'e geçildikten sonra yerelde öksüz kaldı.
    ///
    /// Silinenler — hepsi 192.168.1.101 üzerindeki GERÇEK Siber'e karşı tek tek
    /// sorgulanıp "orada YOK" diye doğrulandı:
    ///
    ///   accounts                Anadolu Nakliyat / Ege Lojistik / Marmara Tedarik
    ///                           (aaaaaaa1-…) — sbr_firma'da 0 kayıt
    ///   cars                    34ABC123 (bbbb1111-…) — skn_arac'ta 0 kayıt.
    ///                           Tek sahibi yukarıdaki taklit carilerden biriydi;
    ///                           bırakılsa boşa işaret eden satır kalırdı.
    ///   loads                   rez-0000-0001 — GUID bile değil
    ///   load_transfers          yuk-0000-0001 / yuk-0000-0002 (siber_id boş —
    ///                           Siber'e hiç yazılmamışlar)
    ///   load_transfer_packages  yukarıdakilerin koli satırları + test-lt-29'un
    ///                           iki satırı (işaret ettiği yük zaten yok)
    ///
    /// DİKKAT — ÖNEK'E GÜVENİLMEZ: "bbbb…" / "7777…" / "dddd…" ile başlayan
    /// GUID'lerin ÇOĞU gerçek Siber kaydı (cars 3222, financial_items 58868,
    /// finance_* tablolarında 34 satır). Taklit ayıklaması önek tahminiyle
    /// DEĞİL, Siber'e sorularak yapılmalı.
    ///
    /// KAPSAM DIŞI: users #133 "Ahmet" (ahmet@siber.test, dddd0000-…) da
    /// Siber'de yok ama kullanıcı silmek ayrı bir karar — dokunulmadı.
    ///
    /// Yedek: storage/backups/taklit-kayit-yedegi-20260901.csv
    /// </summary>
    public partial class RemoveMockSiberRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Eşleşmeler kimliğe DEĞİL, taklit veriden gelen sabit anahtarlara
            // göre yapılır; kimlikler ortamdan ortama değişebiliyor.

            // --- Koli satırları (önce çocuklar) ----------------------------
            migrationBuilder.Sql("""
                DELETE FROM load_transfer_packages
                WHERE load_transfer_id IN ('yuk-0000-0001', 'yuk-0000-0002', 'test-lt-29');
                """);

            // --- Taklit yükler ---------------------------------------------
            // siber_id boş: bu kayıtlar Siber'e hiç yazılmamış, dolayısıyla
            // orada arkada bir şey bırakmıyorlar.
            migrationBuilder.Sql("""
                DELETE FROM load_transfers
                WHERE load_transfer_id IN ('yuk-0000-0001', 'yuk-0000-0002');
                """);

            // --- Taklit teklif ---------------------------------------------
            migrationBuilder.Sql("DELETE FROM loads WHERE siber_id = 'rez-0000-0001';");

            // --- Taklit araç -----------------------------------------------
            // Aşağıdaki carilerden birine bağlı; cari silinince boşa işaret eder.
            migrationBuilder.Sql("""
                DELETE FROM cars WHERE siber_id::text = 'bbbb1111-0000-0000-0000-000000000001';
                """);

            // --- Taklit cariler ve bağlı satırları --------------------------
            migrationBuilder.Sql("""
                DELETE FROM account_type_mappings
                WHERE account_id IN (SELECT id FROM accounts WHERE siber_id::text LIKE 'aaaaaaa1-0000-%');

                DELETE FROM account_contact_people
                WHERE account_id IN (SELECT id FROM accounts WHERE siber_id::text LIKE 'aaaaaaa1-0000-%');

                DELETE FROM account_representatives
                WHERE account_id IN (SELECT id FROM accounts WHERE siber_id::text LIKE 'aaaaaaa1-0000-%');

                DELETE FROM user_account_mappings
                WHERE account_id IN (SELECT id FROM accounts WHERE siber_id::text LIKE 'aaaaaaa1-0000-%');

                DELETE FROM accounts WHERE siber_id::text LIKE 'aaaaaaa1-0000-%';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Geri alınmaz: silinenlerin Siber'de karşılığı yok, geri getirmek
            // yalnızca öksüz satırları geri koyar. Gerekirse yedekten
            // (storage/backups/taklit-kayit-yedegi-20260901.csv) elle alınır.
        }
    }
}
