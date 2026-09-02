using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OLS.DataAccess.Migrations
{
    /// <summary>
    /// SEFERLERDEKİ ŞEHİR KİMLİKLERİNİ ONARIR.
    ///
    /// Senkron, Siber'in <c>sbr_sehir.sehirid</c> değerini
    /// <c>expeditions.start_city_id</c> / <c>load_city_id</c> / <c>end_city_id</c>
    /// sütunlarına DOĞRUDAN yazıyordu. "Yerel cities.id Siber kimliğiyle aynıdır"
    /// varsayımı yalnızca eski 104 Türkiye şehrinin 102'sinde doğruydu; Siber'in
    /// kullandığı yurt dışı şehirleri (MOSKOVA, NOVOROSSIYSK, BAKÜ...) yerel
    /// tabloda hiç yoktu.
    ///
    /// Ölçüm (2026-09-02): 2.824 seferin bitiş şehri, 321'inin başlangıç şehri,
    /// 310'unun yükleme şehri yerelde karşılıksızdı — ekranda güzergâh boş
    /// görünüyordu. Şehir listesi genişletildikten sonra bu satırların tamamı
    /// <c>siber_id</c> üzerinden çözülebiliyor.
    ///
    /// Yedek: storage/backups/sefer-sehir-yedegi-20260902.csv
    /// </summary>
    public partial class RepairExpeditionCityIds : Migration
    {
        private static readonly string[] Columns =
            ["start_city_id", "load_city_id", "end_city_id"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Yalnızca YEREL KARŞILIĞI OLMAYAN kimlikler taşınır; doğru satırlara
            // dokunulmaz. Aynı Siber şehrinin birden çok yerel satırı olabildiği
            // için kimliği Siber kimliğiyle çakışan kanonik satır tercih edilir.
            foreach (var column in Columns)
            {
                migrationBuilder.Sql($"""
                    UPDATE expeditions e
                    SET {column} = (
                        SELECT c.id FROM cities c
                        WHERE lower(c.siber_id) = lower(e.{column}::text)
                        ORDER BY (c.id::text = lower(c.siber_id)) DESC, c.id
                        LIMIT 1)
                    WHERE e.{column} IS NOT NULL
                      AND NOT EXISTS (SELECT 1 FROM cities x WHERE x.id = e.{column})
                      AND EXISTS (SELECT 1 FROM cities c
                                  WHERE lower(c.siber_id) = lower(e.{column}::text));
                    """);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Geri alma: yerel kimlikten Siber kimliğine dönülür.
            foreach (var column in Columns)
            {
                migrationBuilder.Sql($"""
                    UPDATE expeditions e
                    SET {column} = c.siber_id::uuid
                    FROM cities c
                    WHERE c.id = e.{column}
                      AND c.siber_id IS NOT NULL
                      AND lower(c.siber_id) <> lower(c.id::text);
                    """);
            }
        }
    }
}
