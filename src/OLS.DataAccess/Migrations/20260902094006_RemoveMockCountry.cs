using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OLS.DataAccess.Migrations
{
    /// <summary>
    /// TAKLİT SİBER'DEN KALAN ÜLKE SATIRINI KALDIRIR.
    ///
    /// Yerel <c>countries</c> tablosunun 197 satırından 196'sı Siber'in
    /// <c>sbr_ulke</c> tablosunda karşılık buluyor; kalan tek satır "Rusya"
    /// (<c>siber_id = 22222222-2222-2222-2222-222222222223</c>) taklit Siber'in
    /// örnek verisinden gelmiş. Gerçek karşılığı "RUSYA FEDERASYONU" olarak
    /// zaten listede.
    ///
    /// NEDEN ÖNEMLİ: yükte ülke Siber'e ADIYLA ve KITASIYLA yazılıyor, ikisi de
    /// <c>sbr_ulke</c>'den çözülüyor. Karşılığı olmayan bir seçim Siber'de ülkesi
    /// BOŞ bir yük bırakırdı. (Doğrulama artık yazımdan önce de yapılıyor, bkz.
    /// SiberReferenceTable.Ulke — bu satır o uyarıyı hiç görmemek için siliniyor.)
    ///
    /// KULLANIMI YOK: loads / load_transfers / accounts / cities / users /
    /// destinations tablolarının hiçbirinde referansı yok (0 satır, ölçüldü).
    /// Yedek: storage/backups/taklit-ulke-yedegi-20260902.csv
    /// </summary>
    public partial class RemoveMockCountry : Migration
    {
        private const string MockCountryId = "b831b71f-6a86-4bf2-a9fc-1c1becb393e9";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Kimliğin YANINDA taklit siber_id'si de aranır: aynı kimlik başka bir
            // kurulumda gerçek bir ülkeye ait olabilir, yanlış satır silinmesin.
            migrationBuilder.Sql($"""
                DELETE FROM countries
                WHERE id = '{MockCountryId}'
                  AND siber_id = '22222222-2222-2222-2222-222222222223'
                  AND NOT EXISTS (SELECT 1 FROM loads l
                                  WHERE l.departure_country_id = countries.id
                                     OR l.target_country_id = countries.id
                                     OR l.transit_country_id = countries.id);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"""
                INSERT INTO countries (id, name, country_code, phone_code, siber_id)
                VALUES ('{MockCountryId}', 'Rusya', 'RU', '7',
                        '22222222-2222-2222-2222-222222222223')
                ON CONFLICT (id) DO NOTHING;
                """);
        }
    }
}
