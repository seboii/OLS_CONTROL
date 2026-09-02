using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OLS.DataAccess.Migrations
{
    /// <summary>
    /// ŞEHİR LİSTESİNDEN "SİBER'DE BULUNAMADI" HATASINI KALDIRIR.
    ///
    /// Kullanıcı İstanbul'u seçtiğinde sefer kaydı "Başlangıç şehri (Siber'de
    /// bulunamadı)" ile reddediliyordu. Sebep TAKLİT SİBER'DEN KALAN İKİ SATIR:
    /// <c>infra/docker/siber-mock/sample-data.sql</c> İstanbul'a
    /// <c>33333333-…3333</c>, İzmir'e <c>33333333-…3334</c> kimliğini veriyor ve
    /// uygulama bir ara mock'a bağlıyken bunlar yerel <c>cities</c> tablosuna
    /// sızmış. Gerçek Siber'de bu iki kimlik YOK (canlıda doğrulandı: yerel 353
    /// şehir kimliğinin 351'i <c>sbr_sehir</c>'de var, bu ikisi yok).
    ///
    /// İki satır da yoğun kullanımda: 2.910 sefer ve 1.573 cari bunlara işaret
    /// ediyor — bu yüzden SİLİNMEDEN ÖNCE referanslar taşınıyor.
    ///
    /// İSTANBUL ÖZEL DURUM: satırın YEREL kimliği (<c>d04b45bc-…</c>) aslında
    /// Siber'in gerçek İSTANBUL <c>sehirid</c>'si; yanlış olan yalnızca
    /// <c>siber_id</c> sütunu. Bu yüzden satır silinmiyor, kimliği düzeltiliyor —
    /// böylece 2.910 seferin ve 1.573 carinin referansı hiç oynamıyor.
    ///
    /// BİRLEŞTİRME YALNIZCA SİBER KİMLİĞİNE GÖRE. Ada göre birleştirmek YANLIŞ:
    /// Siber'in kendi tablosunda aynı adı taşıyan AYRI şehirler var (iki MINSK,
    /// iki Saint Petersburg, iki TRABZON, iki MERSİN...) ve bunlar farklı
    /// <c>sehirid</c> taşıyor. Ada göre birleştiren ilk sürüm 12 gerçek şehri
    /// listeden düşürmüştü; bkz. RestoreCitiesMergedByName.
    ///
    /// Yedek: storage/backups/sehir-birlestirme-oncesi-20260902.csv
    /// </summary>
    public partial class MergeDuplicateCities : Migration
    {
        private const string MockIstanbul = "33333333-3333-3333-3333-333333333333";
        private const string MockIzmir = "33333333-3333-3333-3333-333333333334";

        private static readonly (string Table, string Column)[] References =
        [
            ("expeditions", "start_city_id"),
            ("expeditions", "load_city_id"),
            ("expeditions", "end_city_id"),
            ("accounts", "city_id"),
            ("destinations", "city_id"),
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) İSTANBUL: yerel kimlik zaten Siber'in kimliği; siber_id düzeltilir.
            migrationBuilder.Sql($"""
                UPDATE cities
                SET siber_id = upper(id::text), updated_at = now()
                WHERE lower(siber_id) = '{MockIstanbul}';
                """);

            // 2) Aynı SİBER KİMLİĞİNİ taşıyan kopyalar tek satıra indirilir.
            //    Kanonik = kimliği kendi siber_id'siyle çakışan satır (Siber'in
            //    kendi kimliği), yoksa en eski satır.
            migrationBuilder.Sql("""
                CREATE TEMP TABLE sehir_birlestirme AS
                WITH kanonik AS (
                    SELECT DISTINCT ON (lower(siber_id)) lower(siber_id) AS anahtar, id
                    FROM cities
                    WHERE siber_id IS NOT NULL AND siber_id <> ''
                    ORDER BY lower(siber_id), (id::text = lower(siber_id)) DESC, id
                )
                SELECT c.id AS eski, k.id AS yeni
                FROM cities c
                JOIN kanonik k ON k.anahtar = lower(c.siber_id)
                WHERE c.id <> k.id;
                """);

            // 3) TAKLİT İZMİR: Siber'de karşılığı yok. Referansları, Siber'de
            //    gerçekten var olan aynı adlı şehre taşınır.
            migrationBuilder.Sql($"""
                INSERT INTO sehir_birlestirme (eski, yeni)
                SELECT m.id, g.id
                FROM cities m
                JOIN cities g ON lower(g.name) = lower(m.name)
                             AND g.id <> m.id
                             AND g.id::text = lower(g.siber_id)
                WHERE lower(m.siber_id) = '{MockIzmir}';
                """);

            foreach (var (table, column) in References)
            {
                migrationBuilder.Sql($"""
                    UPDATE {table} t SET {column} = b.yeni
                    FROM sehir_birlestirme b
                    WHERE t.{column} = b.eski;
                    """);
            }

            // districts.city_id metin sütunu — ayrı ele alınır.
            migrationBuilder.Sql("""
                UPDATE districts d SET city_id = b.yeni::text
                FROM sehir_birlestirme b
                WHERE lower(d.city_id) = lower(b.eski::text);
                """);

            migrationBuilder.Sql("""
                DELETE FROM cities c USING sehir_birlestirme b WHERE c.id = b.eski;
                DROP TABLE sehir_birlestirme;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Birleştirme geri alınamaz (hangi referansın hangi kopyaya ait
            // olduğu bilgisi kayboldu); yedek CSV üzerinden elle dönülür.
        }
    }
}
