using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OLS.DataAccess.Migrations
{
    /// <summary>
    /// ADA GÖRE BİRLEŞTİRİLİRKEN DÜŞEN GERÇEK ŞEHİRLERİ GERİ GETİRİR.
    ///
    /// MergeDuplicateCities'in ilk sürümü kopyaları ADA göre birleştiriyordu.
    /// Ama Siber'in kendi <c>sbr_sehir</c> tablosunda aynı adı taşıyan AYRI
    /// şehirler var — iki MINSK, iki "Saint Petersburg", iki TRABZON, iki
    /// MERSİN, iki BAKÜ... — ve her biri farklı <c>sehirid</c> taşıyor. Ada
    /// göre birleştirme bu 12 gerçek şehri listeden düşürdü.
    ///
    /// Hepsinin Siber'de var olduğu tek tek doğrulandı; kayıtlar ÖZGÜN yerel
    /// kimlikleriyle geri konuyor. Zaten var olan satıra dokunulmaz, yani
    /// migrasyon yinelenebilir ve ada göre birleştirmeyi hiç yaşamamış bir
    /// veritabanında hiçbir şey yapmaz.
    ///
    /// Kaynak: storage/backups/sehir-birlestirme-oncesi-20260902.csv
    /// </summary>
    public partial class RestoreCitiesMergedByName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO cities (id, name, country_id, siber_id, created_at, updated_at)
                SELECT v.id::uuid, v.name, v.country_id, v.siber_id, now(), now()
                FROM (VALUES
                     ('a6af3a83-4962-4bb8-8d2b-7a3abba8e0ea', 'BAKÜ', '242f6eb1-30fa-44f2-ac0b-13329cd63d7a', 'D20CBD7A-0B39-11D6-BD31-0050DA33B050'),
                     ('5a235912-ace9-46e8-8f15-4f7e79a20621', 'BERLIN', '7fdcd4d7-4caf-43e0-b461-0374f7cd81e7', '693150C6-389F-4157-BBF4-002B46695C1C'),
                     ('f951ffb7-45e8-49b8-aa24-d7d632779331', 'MERSİN', 'eb3f2dbe-96fe-4b17-9947-c0ad63af76ca', 'f951ffb7-45e8-49b8-aa24-d7d632779331'),
                     ('dee3968e-7432-4081-8283-897e0be83ee5', 'MINSK', 'd74add13-c511-49b7-8b49-312f3db8c312', '8D316AE4-6FB8-11D6-B5A8-0080AD1754A3'),
                     ('7e821e83-c6a8-44e3-b3a6-031b3a7f2712', 'Minsk', 'd74add13-c511-49b7-8b49-312f3db8c312', 'C87ABE31-DA32-4B0B-97B8-15B14747FE69'),
                     ('64f1e13a-f447-4a86-b76c-6e12fe7092c5', 'MİNKS', '14054599-5c16-4c40-ace5-14d880bde59e', 'F07C808E-61C4-4DE9-B507-AD411ECD5B0F'),
                     ('b5bd91b1-f199-47f0-9def-08108e291af2', 'RIGA', 'ec42841e-15d9-4468-a598-57e1eee15318', '29002DF6-707A-4AF8-BA1A-C847AB752426'),
                     ('1a424e38-39b0-4238-9d09-93ad13901161', 'SAMSUN', 'eb3f2dbe-96fe-4b17-9947-c0ad63af76ca', '1a424e38-39b0-4238-9d09-93ad13901161'),
                     ('c3e26b35-0e07-4c02-83a1-46de01717e03', 'Saint Petersburg', '14054599-5c16-4c40-ace5-14d880bde59e', '30AC0CEF-D65E-4D06-A440-3939F6BFE070'),
                     ('d953c163-08c6-11d6-bd31-0050da33b050', 'SİNOP', 'eb3f2dbe-96fe-4b17-9947-c0ad63af76ca', 'd953c163-08c6-11d6-bd31-0050da33b050'),
                     ('a399f089-11a9-4387-aae7-6bfaa945cbe4', 'TRABZON', 'e98225c2-2434-488d-85d3-db583fdb0fce', 'a399f089-11a9-4387-aae7-6bfaa945cbe4'),
                     ('d055229b-e856-44c7-9707-ea8d07b48236', 'ZONGULDAK', 'e98225c2-2434-488d-85d3-db583fdb0fce', 'd055229b-e856-44c7-9707-ea8d07b48236')
                     ) AS v(id, name, country_id, siber_id)
                WHERE NOT EXISTS (SELECT 1 FROM cities c WHERE c.id = v.id::uuid)
                  AND NOT EXISTS (SELECT 1 FROM cities c WHERE lower(c.siber_id) = lower(v.siber_id));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Geri alma yok: bu satırlar Siber'de gerçekten var.
        }
    }
}
