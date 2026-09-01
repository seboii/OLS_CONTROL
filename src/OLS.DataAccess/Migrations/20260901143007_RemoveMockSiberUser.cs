using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OLS.DataAccess.Migrations
{
    /// <summary>
    /// Taklit Siber'den kalan test kullanıcısını siler:
    /// #133 "Ahmet" — <c>ahmet@siber.test</c>, <c>dddd0000-…</c>.
    ///
    /// Kaynağı <c>infra/docker/siber-mock/sample-data.sql</c>; uygulama bir ara
    /// mock'a bağlıyken içe aktarılmış. Gerçek Siber'in kullanıcı tablosunda
    /// (<c>sky_kullanici</c> — içe aktarma da BURAYI okur) 0 kayıt, dolayısıyla
    /// senkron bu satırı geri getirmez.
    ///
    /// Ayak izi yalnızca 28 <c>user_permissions</c> satırıydı ve HEPSİ
    /// read/update/create/delete = 0, yani hiç erişimi yoktu. Hareket, denetim
    /// kaydı, görevli ataması, cari eşlemesi ya da hedef kaydı YOK — bu yüzden
    /// silmek geride iz bırakmıyor.
    ///
    /// Yedek: storage/backups/taklit-kullanici-yedegi-20260901.csv
    /// </summary>
    public partial class RemoveMockSiberUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Kimlik ortamdan ortama değişebildiği için eşleşme taklit veriden
            // gelen SABİT çift üzerinden: Siber kimliği + e-posta. İkisi birden
            // tutmazsa hiçbir şey silinmez.
            migrationBuilder.Sql("""
                DELETE FROM user_permissions
                WHERE user_id IN (
                    SELECT id FROM users
                    WHERE siber_id::text = 'dddd0000-0000-0000-0000-000000000001'
                      AND email = 'ahmet@siber.test');

                DELETE FROM users
                WHERE siber_id::text = 'dddd0000-0000-0000-0000-000000000001'
                  AND email = 'ahmet@siber.test';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Geri alınmaz: kullanıcının Siber'de karşılığı yok, geri getirmek
            // yalnızca öksüz satırı geri koyar. Gerekirse yedekten
            // (storage/backups/taklit-kullanici-yedegi-20260901.csv) elle alınır.
        }
    }
}
