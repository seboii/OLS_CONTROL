using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OLS.DataAccess.Migrations
{
    /// <summary>
    /// Canlı veritabanına entegrasyon testinden düşmüş iki cariyi siler:
    /// "Workflow Test Musteri 1787640375" ve "Draft Test Musteri 1787647899"
    /// (2026-08-31).
    ///
    /// Silmek güvenli: ikisinin de Siber'de karşılığı YOK (sbr_firma'da 0 kayıt)
    /// ve <c>sbr_log</c>'ta silme kaydı da yok — yani hiç var olmamışlar, yerelde
    /// açılmışlar. Bağlı yük/teklif kaydı, ilgili kişi ya da temsilci yok;
    /// yalnızca kendi cari tipi eşlemeleri var, onlar da birlikte gidiyor.
    ///
    /// Yedek: storage/backups/test-cari-yedegi-20260902.csv
    /// </summary>
    public partial class RemoveTestAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Eşleşme kimliğe değil, taklit veriden gelen SABİT Siber kimliğine
            // göre — kimlikler ortamdan ortama değişebiliyor.
            migrationBuilder.Sql("""
                DELETE FROM account_type_mappings
                WHERE account_id IN (
                    SELECT id FROM accounts WHERE lower(siber_id) IN (
                        'abad65e0-7010-4762-8e67-0296097c2f20',
                        'abed8564-46fc-400d-bbac-e6ed00b710f8'));

                DELETE FROM accounts
                WHERE lower(siber_id) IN (
                    'abad65e0-7010-4762-8e67-0296097c2f20',
                    'abed8564-46fc-400d-bbac-e6ed00b710f8');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Geri alınmaz: kayıtların Siber'de karşılığı yok, geri getirmek
            // yalnızca öksüz satırları geri koyar. Gerekirse yedekten alınır.
        }
    }
}
