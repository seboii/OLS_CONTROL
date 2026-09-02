using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OLS.DataAccess.Migrations
{
    /// <summary>
    /// İKİ İŞ BİR ARADA — ikisi de ülke entegrasyonunun parçası:
    ///
    /// 1. <c>load_transfers.transit_country_id</c> eklenir. Transit ülke teklifsiz
    ///    yük formunda toplanıyor ama hiçbir yere yazılmıyordu; Siber'in
    ///    <c>skn_yuk</c> tablosunda karşılığı YOK (400 sütun tarandı), bu yüzden
    ///    en azından yerel kayıtta korunur.
    ///
    /// 2. Tekliflerde yanlış ülke kimliği ONARILIR. Senkron, Siber'in
    ///    <c>skn_rezervasyon.yuklemeulkeid</c> değerini <c>loads</c> tablosuna
    ///    DOĞRUDAN yazıyordu; "yerel countries.id Siber GUID'iyle aynıdır"
    ///    varsayımı 197 ülkenin 171'inde doğru, 26'sında yanlış. Sonuç: 1.778
    ///    teklifin yükleme ülkesi ve 670'inin varış ülkesi yerelde var olmayan bir
    ///    kimliğe işaret ediyor, ekranda alan BOŞ görünüyordu (yalnızca "TURKYE"
    ///    1.454 kayıt). Yedek: storage/backups/teklif-ulke-yedegi-20260902.csv
    /// </summary>
    public partial class AddLoadTransferTransitCountry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "transit_country_id",
                table: "load_transfers",
                type: "character varying(191)",
                maxLength: 191,
                nullable: true);

            // Yalnızca YEREL KARŞILIĞI OLMAYAN kimlikler taşınır; doğru olan
            // satırlara dokunulmaz. Aynı Siber ülkesinin birden çok yerel satırı
            // olabildiği için ("TÜRKİYE" ve "Türkiye" aynı siber_id'yi taşıyor)
            // kimliği Siber kimliğiyle ÇAKIŞAN kanonik satır tercih edilir.
            foreach (var column in new[] { "departure_country_id", "target_country_id" })
            {
                migrationBuilder.Sql($"""
                    UPDATE loads l
                    SET {column} = (
                        SELECT c.id FROM countries c
                        WHERE lower(c.siber_id) = lower(l.{column}::text)
                        ORDER BY (c.id::text = lower(c.siber_id)) DESC, c.id
                        LIMIT 1)
                    WHERE l.{column} IS NOT NULL
                      AND NOT EXISTS (SELECT 1 FROM countries x WHERE x.id = l.{column})
                      AND EXISTS (SELECT 1 FROM countries c
                                  WHERE lower(c.siber_id) = lower(l.{column}::text));
                    """);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "transit_country_id",
                table: "load_transfers");
        }
    }
}
